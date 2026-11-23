namespace LoadTemplates.Domain.Shared;

using Scaffold.Domain.Shared;

public sealed record TemplatesInput(string Folder, IReadOnlyList<TemplateSet> Sets);
