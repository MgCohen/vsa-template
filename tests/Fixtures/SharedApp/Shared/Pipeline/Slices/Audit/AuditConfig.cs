namespace Pipeline.Slices.Audit;

// A helper type of the Shared base, not an event handler.
public sealed record AuditConfig(long FromId);
