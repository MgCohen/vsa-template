using BadApp.Modules.Billing.Domain;
using BadApp.Modules.Billing.Domain.Shared;
using VSA.Infrastructure;

namespace BadApp.Modules.Billing.Infrastructure;

public sealed class InvoiceRepository(InvoiceStore store) : IRepository<Invoice, InvoiceId>
{
    public Task<Invoice?> GetAsync(InvoiceId id, CancellationToken ct) => Task.FromResult(store.Invoices.GetValueOrDefault(id));

    public Task<List<Invoice>> ListAsync(Func<Invoice, bool> where, CancellationToken ct) => Task.FromResult(store.Invoices.Values.Where(where).ToList());

    public void Add(Invoice aggregate) => store.Invoices[aggregate.Id] = aggregate;

    public void Remove(Invoice aggregate) => store.Invoices.TryRemove(aggregate.Id, out _);

    public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
}
