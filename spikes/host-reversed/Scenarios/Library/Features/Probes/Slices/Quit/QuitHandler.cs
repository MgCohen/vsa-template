namespace Greeter.Library.Features.Probes.Slices.Quit;

using Spike.Kernels;
using Spike.Pack;

public sealed class QuitHandler(AppStop appStop) : RequestHandler<QuitRequest, QuitResponse> {
    public override Task<QuitResponse> Handle(QuitRequest request, CancellationToken ct) {
        appStop.Request(request.ExitCode);
        return Task.FromResult(new QuitResponse($"stop requested with {request.ExitCode}"));
    }
}
