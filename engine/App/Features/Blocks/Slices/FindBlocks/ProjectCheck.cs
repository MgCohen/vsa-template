namespace Blocks.Slices.FindBlocks;

using Blocks.Domain;
using Blocks.Infrastructure;
using Operations.Domain.Shared;

static class ProjectCheck {
    public static IReadOnlyList<Finding> Findings(
        string folder, IReadOnlyList<BlockProject> projects, bool hasConfigFile) {
        var findings = projects.Where(project => !project.HasUmpirePackage).Select(NoUmpirePackage).ToList();
        if (projects.Count > 1 && !hasConfigFile) {
            findings.Add(new Finding(BlocksCodes.NoConfigFile, FindingSeverity.Warning,
                $"The block {folder} has {projects.Count} projects and no {EntryFiles.Name} file. "
                + $"Add a {EntryFiles.Name} file to the block folder: it is the root of the build output "
                + "and of the rule values of all projects of the block.", folder));
        }
        return findings;
    }

    public static Finding NoDefinitionProject(string folder) =>
        new(BlocksCodes.NoDefinitionProject, FindingSeverity.Warning,
            $"The Definition folder of the block {folder} has source files and no project, so the definition is not in "
            + "its own check-time DLL. Add a project to the folder, for example <Name>.Definition.csproj.",
            folder);

    static Finding NoUmpirePackage(BlockProject project) =>
        new(BlocksCodes.NoUmpirePackage, FindingSeverity.Warning,
            $"The project {project.File} has no Umpire package, so it does not get the Umpire build. Add a "
            + "PackageReference to an Umpire package, for example "
            + "<PackageReference Include=\"Umpire.Vsa\" Version=\"<version>\" />, "
            + "or Umpire.Web for a web project.",
            project.File);
}
