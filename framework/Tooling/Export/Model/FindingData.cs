namespace Viewer.Export.Model;

public sealed record FindingData(string Code, string File, int Line, string Message, string? Type);
