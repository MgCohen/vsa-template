using Microsoft.Extensions.Hosting;
using VSA.Composition;

await VsaBuilder.Create(args).UseGenericHost().Build().RunAsync();
