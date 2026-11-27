namespace CodeCheck.Slices.CheckCode;

using Facts.Domain.Shared.Compile;

public sealed class RecordPayloadRule : VsaRule {
    public const string CheckResponsesKey = "check_responses";

    public override int Number => 1;

    public override IReadOnlyList<RuleOption> Options { get; } = [RuleOption.Bool(CheckResponsesKey, true)];

    public override string Title => "Requests and responses are records";

    public override string Message =>
        "{0} is {1}. Make it a record.";

    public override IEnumerable<CodeFinding> Check(CodeType type, RuleOptions options) {
        if (type.IsAbstract || !type.IsClassOrStruct || type.Request is not { } request) {
            yield break;
        }
        if (!type.IsRecord) {
            yield return Found(type.Name.Place, type.Name.Name, "a request");
        }
        if (options.Bool(CheckResponsesKey) != true || request.HasConcreteBase) {
            yield break;
        }
        if (request.Response is { IsRecord: false } response) {
            yield return Found(response.Name.Place, response.Name.Name, $"the response of {type.Name.Name}");
        }
    }
}
