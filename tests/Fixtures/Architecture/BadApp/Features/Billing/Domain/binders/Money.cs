namespace BadApp.Billing.Domain.Binders;

public sealed record Money(decimal Amount) : Kit.IEntity;
