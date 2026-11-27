namespace Facts.Domain.Shared.Compile;

public sealed record CodeRequest(bool HasConcreteBase, CodeResponse? Response);
