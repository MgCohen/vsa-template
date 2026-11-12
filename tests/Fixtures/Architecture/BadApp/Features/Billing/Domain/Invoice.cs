namespace BadApp.Billing.Domain;

public sealed record Invoice(Guid Id, decimal Amount) : Kit.IEntity;
