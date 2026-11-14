using System.Text.Json;
using System.Text.Json.Serialization;
using ArchitectureViewer.Domain;
using ArchitectureViewer.Domain.Definitions;
using ArchitectureViewer.Domain.Shared;

namespace ArchitectureViewer.Infrastructure;

public static class ChangesJson
{
    private static readonly JsonSerializerOptions options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Write(Definition definition, IReadOnlyList<RuleChange> changes, IReadOnlyList<WhatIf> whatIf)
    {
        List<object> items = [.. changes.Select(c => ChangeOf(definition, c))];
        return JsonSerializer.Serialize(new FileDto(1, definition.Name, items, whatIf), options);
    }

    private static object ChangeOf(Definition definition, RuleChange change)
    {
        IEnumerable<ConditionDto> conditions = RuleChanges.Conditions(change).Select(ConditionOf);
        if (change.Op == RuleOp.AddRule)
        {
            string op = change.Op.ToString();
            ConditionDto? condition = conditions.FirstOrDefault();
            return new AddRuleDto(op, change.From.Layer, change.To.Layer, Verb(change.Access), condition);
        }
        string rule = definition.Rules.First(r => r.Id == change.RuleId).Text;
        return new EditRuleDto(
            change.Op.ToString(), rule, change.From.Layer, change.To.Layer, Verb(change.Access), [.. conditions]);
    }

    private static string Verb(string access)
    {
        return access == Access.Deny ? "Denies" : "Uses";
    }

    private static ConditionDto ConditionOf(Condition condition)
    {
        return new(condition.Kind, condition.Layer);
    }

    private sealed record FileDto(
        int Version, string Definition, IReadOnlyList<object> Changes, IReadOnlyList<WhatIf> WhatIf);

    private sealed record AddRuleDto(string Op, string From, string To, string Access, ConditionDto? Condition);

    private sealed record EditRuleDto(
        string Op, string Rule, string From, string To, string Access, IReadOnlyList<ConditionDto> Conditions);

    private sealed record ConditionDto(string Kind, string Layer);
}
