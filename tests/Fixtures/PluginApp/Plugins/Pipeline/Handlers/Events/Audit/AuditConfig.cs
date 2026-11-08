namespace Pipeline.Handlers.Events.Audit;

// A helper type of the plugin base, not an event handler. A subfolder of a layer folder goes to the same project.
public sealed record AuditConfig(long FromId);
