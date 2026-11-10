using Microsoft.Extensions.DependencyInjection;
using BadApp.Modules.Billing.Domain;
using BadApp.Modules.Billing.Domain.Shared;
using BadApp.Modules.Billing.Infrastructure;
using VSA.Infrastructure;
using VSA.Module;

namespace BadApp.Modules.Billing;

public sealed class BillingModule : Module
{
    public override void Configure(ModuleBuilder module)
    {
        module.Services.AddSingleton<InvoiceStore>();
        module.Services.AddScoped<IRepository<Invoice, InvoiceId>, InvoiceRepository>();
    }
}
