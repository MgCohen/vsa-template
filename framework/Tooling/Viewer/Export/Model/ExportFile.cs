namespace Viewer.Export.Model;

public sealed record ExportFile(int Version, DefinitionData Definition, AppData App);
