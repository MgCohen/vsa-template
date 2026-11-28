namespace CodeCheck;

using CodeCheck.Slices.CheckCode;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;

public sealed class CodeCheckModule : Module {
    protected override void Configure(IServiceCollection services) => services.AddSingleton<CodeRuns>();
}
