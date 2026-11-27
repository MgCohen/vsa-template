using Umpire.Hosts.Web;
using SampleApp.Host;

return await new SampleAppApp().Run(new WebHost(args));
