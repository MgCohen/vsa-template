namespace Model.Slices.CheckModel;

using Facts.Infrastructure;
using Operations.Domain.Shared;
using Umpire.CheckModel;
using Umpire.Config;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;
using BlockFolders = Umpire.Architecture.BlockFolders;
using ModelFinding = Umpire.CheckModel.Finding;

sealed class CheckModelHandler(IFactSource<BlockFolderQuery, BlockFolders> folders) : CommandHandler<CheckModelCommand, CheckModelResult> {
    public override Task<Result<CheckModelResult>> HandleAsync(IEnvelope<CheckModelCommand> envelope) =>
        Task.FromResult<Result<CheckModelResult>>(Check(envelope.Message));

    CheckModelResult Check(CheckModelCommand command) {
        CheckMode? given = command.Mode is { } mode ? mode == ReportMode.Error ? CheckMode.Error : CheckMode.Warn : null;
        StateFolder? state = null;
        Report report;
        try {
            state = StateOf(command);
            report = StateCheck.Run(state, folders, given);
        } catch (Exception e) when (e is not OperationCanceledException) {
            report = new Report();
            report.Fail($"{e.GetType().Name}: {e.Message}");
        }
        var effective = given ?? SavedMode(state, command.Block);
        var findings = report.Findings.Select(Converted).ToList();
        if (report.Reason is { } reason) {
            findings.Add(new Operations.Domain.Shared.Finding(DataCodes.NoModel,
                effective == CheckMode.Warn ? FindingSeverity.Warning : FindingSeverity.Error,
                $"The check did not run to its end: {reason}"));
        }
        return new CheckModelResult(findings, effective == CheckMode.Error ? ReportMode.Error : ReportMode.Warn);
    }

    static CheckMode SavedMode(StateFolder? state, string block) {
        try {
            return state is null ? ConfiguredMode(block) : state.ReadBlock().Mode;
        } catch (Exception e) when (e is not OperationCanceledException) {
            return ConfiguredMode(block);
        }
    }

    static CheckMode ConfiguredMode(string block) {
        try {
            return CheckModes.Of(UmpireConfig.Find(block).Value(CheckModes.Key));
        } catch (Exception e) when (e is not OperationCanceledException) {
            return CheckModes.Default;
        }
    }

    static StateFolder StateOf(CheckModelCommand command) {
        if (command.State.Length > 0) {
            return StateFolder.At(Path.GetFullPath(command.State));
        }
        var block = Path.GetFullPath(command.Block);
        var config = UmpireConfig.Find(block);
        return config.Value(StateFolder.Key) is { Length: > 0 } configured
            ? StateFolder.Of(Path.GetDirectoryName(config.FilePath!)!, configured)
            : StateFolder.Of(block);
    }

    internal static Operations.Domain.Shared.Finding Converted(ModelFinding finding) => new(
        finding.Code, SeverityOf(finding.Severity), finding.Text, finding.File, finding.File is null ? null : finding.Line,
        finding.File is null ? null : finding.Column);

    static FindingSeverity SeverityOf(Severity severity) => severity switch {
        Severity.Error => FindingSeverity.Error,
        Severity.Warning => FindingSeverity.Warning,
        _ => FindingSeverity.Info,
    };
}
