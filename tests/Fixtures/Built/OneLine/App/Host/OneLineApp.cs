namespace OneLine.App.Host;

using Umpire.Kernels;
using Umpire.Vsa;

public sealed class OneLineApp : App<Vsa<AppHost>> {
    public const string Name = "a line that is longer than sixty characters";
}
