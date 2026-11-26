namespace Greeter.Library.Features.Probes.Slices.Quit;

using Spike.Kernels;

public sealed record QuitRequest(int ExitCode) : IRequest<QuitResponse>;
