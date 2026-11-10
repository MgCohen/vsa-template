using Microsoft.Extensions.DependencyInjection;
using GoodApp.Modules.Billing.Domain;
using GoodApp.Modules.Billing.Domain.Shared;
using GoodApp.Modules.Billing.Infrastructure;
using VSA.Infrastructure;
using VSA.Module;

namespace GoodApp.Modules.Billing;

public sealed class BillingModule : Module
{
    public override void Configure(ModuleBuilder module)
    {
        module.Services.AddSingleton<InvoiceStore>();
        module.Services.AddScoped<IRepository<Invoice, InvoiceId>, InvoiceRepository>();
    }
}
