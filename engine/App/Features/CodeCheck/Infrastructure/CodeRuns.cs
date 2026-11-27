namespace CodeCheck.Infrastructure;

using System.Runtime.CompilerServices;
using CodeCheck.Slices.CheckCode;
using Facts.Domain.Shared.Compile;
using Facts.Infrastructure;

public sealed class CodeRuns(IFactSource<PackQuery, bool> packs) {
    readonly ConditionalWeakTable<CompileProject, CodeRun> runs = new();

    public CodeRun Of(CompileProject project) =>
        runs.GetValue(project, given => CodeRun.Start(given, (architecture, pack) => packs.Read(new PackQuery(given, architecture, pack))));
}
