using BuiltApp.Infrastructure;
using LoadTemplates.Domain.Shared;
using Scaffold.Infrastructure;
using Umpire.Architecture;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

namespace LoadTemplates.Slices.LoadTemplates;

sealed class LoadTemplatesHandler : CommandHandler<LoadTemplatesCommand, TemplatesInput> {
    public override Task<Result<TemplatesInput>> HandleAsync(Envelope<LoadTemplatesCommand> envelope) =>
        Task.FromResult(Load(envelope.Message));

    static Result<TemplatesInput> Load(LoadTemplatesCommand command) {
        if (command.Folder.Length == 0 || !Directory.Exists(command.Folder) || !Directory.Exists(command.Bin)) {
            return new Error(LoadTemplatesCodes.NoTemplateBlock,
                $"The template block '{command.Folder}' or its output folder '{command.Bin}' does not exist. "
                + "Give both with --scaffoldtemplates and --scaffoldtemplatesbin, and build the template block first.");
        }
        var folder = Path.GetFullPath(command.Folder);
        var dlls = Directory.GetFiles(Path.GetFullPath(command.Bin), "*.dll")
            .Where(dll => SourceFiles.HasSourceUnder(dll, folder))
            .ToList();
        if (dlls.Count == 0) {
            return new Error(LoadTemplatesCodes.NoTemplateAssembly,
                $"No assembly in {command.Bin} has source files under the template block {folder}. "
                + "Build the template block, and give its output folder.");
        }
        var resolved = new ResolvedArchitecture(command.App.Architecture);
        var templates = AppReader.Read(dlls, folder, resolved);
        var sets = TemplateSets.In(resolved.Place(templates.Facts, templates.BlockFolder));
        if (sets.Count == 0) {
            return new Error(LoadTemplatesCodes.NoTemplateSet,
                $"The template block {folder} has no template set. A template set is a slice whose name starts with "
                + $"{TemplateSets.SamplePrefix}, with one handler.");
        }
        return new TemplatesInput(folder, sets);
    }
}
