namespace VSA.Domain;

// Code format: "<module>.<reason>", for example "notes.not_found".
public sealed record Error(string Code, string Message);
