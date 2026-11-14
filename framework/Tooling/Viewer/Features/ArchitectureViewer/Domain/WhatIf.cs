namespace ArchitectureViewer.Domain;

public sealed record WhatIf(string Op, string From, string FromLayer, string To, string ToLayer, string Note)
{
    public const string AppLink = "App view link. It is not a rule change.";
}
