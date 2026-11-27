namespace Facts.Domain.Shared.Compile;

public sealed record CodeType(
    string FullName, string Assembly, CodeName Name, bool IsAbstract, bool IsClassOrStruct, bool IsRecord, CodeRequest? Request);
