namespace CodeCheck.Slices.CheckCode;

using System.Runtime.CompilerServices;
using Facts.Domain.Shared.Compile;
using Facts.Infrastructure;

sealed class CodeRuns(IFactSource<PackQuery, bool> packs) {
    readonly ConditionalWeakTable<CompileProject, CodeRun> runs = new();

    public CodeRun Of(CompileProject project) =>
        runs.GetValue(project, given => CodeRun.Start(given, (architecture, pack) => packs.Read(new PackQuery(given, architecture, pack))));
}
