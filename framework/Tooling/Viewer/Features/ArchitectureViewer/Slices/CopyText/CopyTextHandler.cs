using ArchitectureViewer.Domain;
using VSA.Contracts;
using VSA.Handlers.Requests;

namespace ArchitectureViewer.Slices.CopyText;

internal sealed class CopyTextHandler(IClipboard clipboard) : RequestHandler<CopyTextRequest, CopyTextResponse>
{
    public override async Task<Result<CopyTextResponse>> HandleAsync(Envelope<CopyTextRequest> envelope)
    {
        return new CopyTextResponse(await clipboard.Copy(envelope.Message.Text));
    }
}
