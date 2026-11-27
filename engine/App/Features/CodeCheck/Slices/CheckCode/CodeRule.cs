namespace CodeCheck.Slices.CheckCode;

using System.Globalization;
using System.Reflection;
using Facts.Domain.Shared.Compile;
using Operations.Domain.Shared;

abstract class CodeRule {
    const string Area = "01";

    static readonly IReadOnlyList<CodeRule> Rules = [.. TypesOf(typeof(CodeRule).Assembly)
        .Where(type => type.IsSubclassOf(typeof(CodeRule)) && !type.IsAbstract)
        .Select(type => (CodeRule)Activator.CreateInstance(type)!)
        .OrderBy(rule => rule.Id, StringComparer.Ordinal)];

    public static IReadOnlyList<CodeRule> All => Rules;

    public static CodeNotice OptionNotice { get; } = new(
        "UMP010001",
        "An Umpire rule option is missing or not valid",
        "Rule {0} needs the option umpire.{0}.{1}, {2}. The value is '{3}'. Set it in the .umpireconfig file of the block. "
            + "{0} does not run until the option is valid.");

    public static CodeNotice ConfigNotice { get; } = new(
        "UMP010002",
        "The .umpireconfig file is not valid TOML",
        "The .umpireconfig file is not valid TOML: {0} Correct the file. The Umpire rules do not run until the file is valid.");

    public static CodeNotice LoadNotice { get; } = new(
        "UMP010003",
        "The Umpire analyzers cannot load a library",
        "The Umpire analyzers cannot load a library: {0} The analyzer DLL embeds Umpire.Config. Use a "
            + "compiler on .NET 8 or later. The Umpire rules do not run until the library loads.");

    public static IReadOnlyList<CodeNotice> Notices { get; } = [OptionNotice, ConfigNotice, LoadNotice];

    public abstract int Group { get; }

    public abstract int Number { get; }

    public string Id => $"UMP{Area}{Group:00}{Number:00}";

    public virtual FindingSeverity Severity => FindingSeverity.Warning;

    public virtual bool IsModuleRule => true;

    public virtual bool IsOnByDefault => true;

    public virtual string? PackNamespace => null;

    public virtual IReadOnlyList<RuleOption> Options => [];

    public abstract string Title { get; }

    public abstract string Message { get; }

    public bool ChecksTypes =>
        GetType().GetMethod(nameof(Check), [typeof(CodeType), typeof(RuleOptions)])?.DeclaringType != typeof(CodeRule);

    public string Text(params object[] arguments) => string.Format(CultureInfo.InvariantCulture, Message, arguments);

    public virtual IEnumerable<CodeFinding> Check(CodeFile file, RuleOptions options) => [];

    public virtual IEnumerable<CodeFinding> Check(CodeType type, RuleOptions options) => [];

    protected CodeFinding Found(CodePlace place, params object[] arguments) => new(place, Text(arguments));

    static IEnumerable<Type> TypesOf(Assembly assembly) {
        try {
            return assembly.GetTypes();
        } catch (ReflectionTypeLoadException e) {
            return e.Types.OfType<Type>();
        }
    }
}
