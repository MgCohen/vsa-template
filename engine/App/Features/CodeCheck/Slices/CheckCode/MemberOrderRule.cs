namespace CodeCheck.Slices.CheckCode;

using Facts.Domain.Shared.Compile;

sealed class MemberOrderRule : KernelRule {
    public const string KindOrderKey = "kind_order";
    public const string AccessOrderKey = "access_order";
    public const string StaticFirstKey = "static_first";
    const string Constant = "constant";

    public override int Number => 5;

    public override IReadOnlyList<RuleOption> Options { get; } =
        [RuleOption.List(KindOrderKey, [Constant, "field", "constructor", "finalizer", "property", "event", "method", "nested_type"]),
            RuleOption.List(AccessOrderKey, ["public", "internal", "protected_internal", "protected", "private_protected", "private"]),
            RuleOption.Bool(StaticFirstKey, true)];

    public override string Title => "Members are ordered by kind, then by access, then static before instance";

    public override string Message =>
        "{0} is a {1}, and it comes after a {2}. Move {0} up. The order is: by kind ({3}), then by access ({4}), then {5}.";

    public override IEnumerable<CodeFinding> Check(CodeFile file, RuleOptions options) =>
        Order.Read(options) is { } order ? file.Types.SelectMany(type => Check(type, order)) : [];

    static string Shown(CodeMember member) =>
        $"{member.Access.Replace('_', ' ')} {(member.IsStatic && member.Kind != Constant ? "static " : "")}{member.Kind.Replace('_', ' ')}";

    IEnumerable<CodeFinding> Check(CodeTypeBody type, Order order) {
        CodeMember? latest = null;
        foreach (var member in type.Members) {
            if (latest is { } previous && order.Compare(member, previous) < 0) {
                yield return Found(member.Name.Place, member.Name.Name, Shown(member), Shown(previous),
                    string.Join(", ", order.Kinds), string.Join(", ", order.Accesses),
                    order.StaticFirst ? "static before instance" : "instance before static");
                continue;
            }
            latest = member;
        }
    }

    sealed class Order(IReadOnlyList<string> kinds, IReadOnlyList<string> accesses, bool staticFirst) {
        public IReadOnlyList<string> Kinds { get; } = kinds;
        public IReadOnlyList<string> Accesses { get; } = accesses;
        public bool StaticFirst { get; } = staticFirst;

        public static Order? Read(RuleOptions options) =>
            options.List(KindOrderKey) is { } kinds && options.List(AccessOrderKey) is { } accesses
            && options.Bool(StaticFirstKey) is { } staticFirst
                ? new Order(kinds, accesses, staticFirst)
                : null;

        public int Compare(CodeMember a, CodeMember b) {
            var kind = Index(Kinds, a.Kind).CompareTo(Index(Kinds, b.Kind));
            if (kind != 0) {
                return kind;
            }
            var access = Index(Accesses, a.Access).CompareTo(Index(Accesses, b.Access));
            if (access != 0) {
                return access;
            }
            return StaticFirst ? b.IsStatic.CompareTo(a.IsStatic) : a.IsStatic.CompareTo(b.IsStatic);
        }

        static int Index(IReadOnlyList<string> list, string name) {
            var index = list.ToList().IndexOf(name);
            return index < 0 ? list.Count : index;
        }
    }
}
