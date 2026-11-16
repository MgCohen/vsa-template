namespace VsaApp.Features.Notebook.Slices.Archive;

using Umpire.Vsa.Contracts.Commands;

public sealed record ArchiveCommand(string Title) : ICommand<ArchiveResult>;
