namespace Facts.Infrastructure;

using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;

sealed record IlToken(int Offset, OpCode Code, EntityHandle Handle);

static class IlCode {
    const byte TwoBytePrefix = 0xFE;
    const int TokenSize = 4;
    const int SwitchTargetSize = 4;

    static readonly Dictionary<short, OpCode> Codes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(field => field.GetValue(null)).OfType<OpCode>().ToDictionary(code => code.Value);

    public static IEnumerable<IlToken> Tokens(MethodBodyBlock body) {
        var reader = body.GetILReader();
        var found = new List<IlToken>();
        while (reader.RemainingBytes > 0) {
            var offset = reader.Offset;
            var first = reader.ReadByte();
            var value = first == TwoBytePrefix && reader.RemainingBytes > 0 ? (short)((first << 8) | reader.ReadByte()) : (short)first;
            if (!Codes.TryGetValue(value, out var code)) {
                break;
            }
            if (IsToken(code.OperandType)) {
                var token = reader.ReadInt32();
                found.Add(new IlToken(offset, code, System.Reflection.Metadata.Ecma335.MetadataTokens.EntityHandle(token)));
                continue;
            }
            if (!Skip(ref reader, code.OperandType)) {
                break;
            }
        }
        return found;
    }

    static bool IsToken(OperandType operand) =>
        operand is OperandType.InlineField or OperandType.InlineMethod or OperandType.InlineType or OperandType.InlineTok;

    static bool Skip(ref BlobReader reader, OperandType operand) {
        var size = operand switch {
            OperandType.InlineNone => 0,
            OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
            OperandType.InlineVar => 2,
            OperandType.InlineI8 or OperandType.InlineR => 8,
            OperandType.InlineSwitch => SwitchSize(ref reader),
            _ => TokenSize,
        };
        if (size < 0 || size > reader.RemainingBytes) {
            return false;
        }
        reader.Offset += size;
        return true;
    }

    static int SwitchSize(ref BlobReader reader) =>
        reader.RemainingBytes < TokenSize ? -1 : reader.ReadInt32() * SwitchTargetSize;
}
