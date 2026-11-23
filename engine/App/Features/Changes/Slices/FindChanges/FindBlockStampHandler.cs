namespace Changes.Slices.FindChanges;

using BuiltApp.Domain.Shared;
using Changes.Domain.Shared;
using Changes.Infrastructure;
using Steps.Domain.Shared;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class FindBlockStampHandler : CommandHandler<FindBlockStampCommand, BlockStampResult> {
    public override Task<Result<BlockStampResult>> HandleAsync(IEnvelope<FindBlockStampCommand> envelope) {
        var command = envelope.Message;
        var block = command.Block;
        var definition = $"{block.Key}|{block.Folder}";
        var dlls = BlockDlls.Of(block, command.Bin);
        var file = BlockStamp.In(block.Folder, BlockHash.View(definition, dlls, command.Run));
        var hash = BlockHash.Inputs(block.Folder, definition, dlls, command.Run);
        var same = BlockStamp.Read(file) == hash;
        Finding[] findings = same
            ? [new Finding(ChangesCodes.BlockSame, FindingSeverity.Info, Skipped(block.Folder))]
            : [];
        return Task.FromResult<Result<BlockStampResult>>(new BlockStampResult(findings, same, file, hash));
    }

    static string Skipped(string folder) =>
        $"The block {folder} did not change since its last clean check, so its check did not run.";
}
