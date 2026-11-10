using BadApp.Modules.Notes.Domain.Shared;

namespace BadApp.Modules.Billing.Shared;

public readonly record struct Money(decimal Amount, NoteId? Note);
