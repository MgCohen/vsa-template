namespace Umpire.Vsa.Hosts.Web;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Umpire.Vsa.Composition;
using Umpire.Kernel;
using Umpire.Vsa.Views;

public sealed class WebVsaHost : VsaHost {
    protected override IHostApplicationBuilder CreateBuilder(string[] args) => WebApplication.CreateBuilder(args);

    protected override void Install(IServiceCollection services, AppTypes types) {
        base.Install(services, types);
        services.AddViews(types);
    }

    protected override IHost Build(IHostApplicationBuilder builder) =>
        ((WebApplicationBuilder)builder).Build().MapRequests().MapViews();
}
