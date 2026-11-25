namespace OneLine.Host;

using Umpire.Kernels;
using Umpire.Vsa.Composition;

public sealed class OneLineApp : App<VsaKernel, AppHost> {
    public const string Name = "a line that is longer than sixty characters";
}
