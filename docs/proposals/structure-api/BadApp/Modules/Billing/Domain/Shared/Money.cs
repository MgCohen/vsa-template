namespace BadApp.Modules.Billing.Shared;

public readonly record struct Money(decimal Amount, BadApp.Modules.Notes.Domain.Shared.NoteId? Note);
