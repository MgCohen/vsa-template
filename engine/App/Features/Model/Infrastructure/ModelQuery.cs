namespace Model.Infrastructure;

using Facts.Domain.Shared.Compile;

public sealed record ModelQuery(CompileProject Project, string Block);
