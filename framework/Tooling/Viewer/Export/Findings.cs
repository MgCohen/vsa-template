using System.Globalization;
using ArchUnitNET.Domain;
using Viewer.Export.Model;
using VSA.Architecture.Engine;

namespace Viewer.Export;

internal static class Findings
{
    private const string Error = ": error ";

    public static IEnumerable<FindingData> Of(ArchitectureMap architecture, IType type, string typeId)
    {
        return architecture.Findings(type).Select(text => Parse(text, typeId));
    }

    private static FindingData Parse(string text, string typeId)
    {
        int error = text.IndexOf(Error, StringComparison.Ordinal);
        string location = text[..error];
        string rest = text[(error + Error.Length)..];
        int colon = rest.IndexOf(':', StringComparison.Ordinal);
        int open = location.LastIndexOf('(');
        string file = open < 0 ? location : location[..open];
        int line = open < 0 ? 0 : int.Parse(location[(open + 1)..^1], CultureInfo.InvariantCulture);
        return new FindingData(rest[..colon], file, line, rest[(colon + 2)..], typeId);
    }
}
