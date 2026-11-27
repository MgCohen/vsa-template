namespace Facts.Domain.Shared.Compile;

public sealed record CodeMember(string Kind, string Access, bool IsStatic, CodeName Name);
