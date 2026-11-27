namespace Jobs.Slices.CompileJob;

using CodeCheck.Slices.CheckCode;
using Facts.Domain.Shared.Compile;
using Model.Slices.CheckTypes;
using Operations.Domain.Shared;
using Operations.Infrastructure;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Vsa.Handlers.Requests;

sealed class CompileJobHandler(ICommandSender commands, OperationLog log)
    : RequestHandler<CompileJobRequest, CompileJobReport> {
    public override async Task<Result<CompileJobReport>> HandleAsync(IEnvelope<CompileJobRequest> envelope) {
        var job = envelope.Message;
        var ct = envelope.Cancellation;
        if (job.Stage == CompileStage.Load) {
            return await CodesOf(job, ct);
        }
        var checksTypes = job.Stage != CompileStage.End
            && await commands.SendAsync(new CheckCodeCommand(job.Project, job.Stage, job.File, job.Type), ct) is { IsSuccess: true } code
            && code.Value.ChecksTypes;
        var inModel = job.Stage != CompileStage.File
            && await commands.SendAsync(new CheckTypesCommand(job.Project, job.Stage, job.Type, job.HasErrors), ct) is { IsSuccess: true } types
            && types.Value.InModel;
        return new CompileJobReport(ReportMode.Warn, [.. log.Findings], inModel, checksTypes || inModel);
    }

    async Task<CompileJobReport> CodesOf(CompileJobRequest job, CancellationToken ct) {
        var code = await commands.SendAsync(new CheckCodeCommand(job.Project, job.Stage), ct);
        var types = await commands.SendAsync(new CheckTypesCommand(job.Project, job.Stage), ct);
        return new CompileJobReport(ReportMode.Warn, [.. log.Findings], false, false) {
            Codes = [.. code.IsSuccess ? code.Value.Codes : [], .. types.IsSuccess ? types.Value.Codes : []],
        };
    }
}
