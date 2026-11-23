using BuiltApp.Infrastructure;
using LoadApp.Domain.Shared;
using Scaffold.Domain.Shared;
using Scaffold.Infrastructure;
using System.Text.RegularExpressions;
using Umpire.Architecture;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

namespace PlanScaffold.Slices.PlanScaffold;

sealed partial class PlanScaffoldHandler : CommandHandler<PlanScaffoldCommand, ScaffoldPlan> {
    public override Task<Result<ScaffoldPlan>> HandleAsync(Envelope<PlanScaffoldCommand> envelope) =>
        Task.FromResult(Plan(envelope.Message));

    static Result<ScaffoldPlan> Plan(PlanScaffoldCommand command) {
        var input = command.Input;
        var sets = command.Templates.Sets;
        var set = sets.FirstOrDefault(s => string.Equals(s.Kind, input.Kind, StringComparison.OrdinalIgnoreCase));
        if (set is null) {
            return new Error(PlanScaffoldCodes.UnknownKind,
                $"No template set has the kind {input.Kind}. The kinds are: {string.Join(", ", sets.Select(s => s.Kind))}.");
        }
        if (!PascalCase().IsMatch(input.Name) || input.Name.Contains(TemplateSets.SamplePrefix, StringComparison.Ordinal)) {
            return new Error(PlanScaffoldCodes.InvalidName,
                $"The {input.Kind} name '{input.Name}' is not valid. Use a PascalCase C# identifier "
                + $"that does not contain {TemplateSets.SamplePrefix}, for example CreateInvoice.");
        }
        var planned = Planner.Plan(set, input, AppBindings(command.App), command.App.BlockFolder);
        if (planned.Problem is { } problem) {
            return new Error(CodeOf(problem.Kind), problem.Message);
        }
        var plan = planned.Plan!;
        var existing = FileSet.Existing(command.App.BlockFolder, plan.Files);
        if (existing.Count > 0) {
            return new Error(PlanScaffoldCodes.FilesExist,
                $"These files exist, and the scaffold does not write over a file: {string.Join(", ", existing)}.");
        }
        return plan;
    }

    static Bindings AppBindings(AppInput app) {
        var resolved = new ResolvedArchitecture(app.Architecture);
        var loaded = AppReader.Read(app.Assemblies.Select(a => a.Location), app.BlockFolder, resolved);
        return resolved.Place(loaded.Facts, loaded.BlockFolder, app.InnerBlocks);
    }

    static string CodeOf(ScaffoldProblemKind kind) => kind switch {
        ScaffoldProblemKind.MissingGroupValue => PlanScaffoldCodes.MissingGroupValue,
        _ => PlanScaffoldCodes.NoGroupFolder,
    };

    [GeneratedRegex("^[A-Z][A-Za-z0-9]*$")]
    private static partial Regex PascalCase();
}
