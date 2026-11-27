namespace Facts.Infrastructure;

using Facts.Domain.Shared.Compile;

public sealed record PackQuery(CompileProject Project, string? Architecture, string PackNamespace);
