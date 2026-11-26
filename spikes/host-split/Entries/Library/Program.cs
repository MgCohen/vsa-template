using Greeter;
using Greeter.Features.Greeting;
using Greeter.Features.Greeting.Slices.Greet;
using Spike.Kernels;
using Spike.Pack;

var app = new GreeterApp();

var local = app.Start(new LocalHost());
Console.WriteLine(local.Send(new GreetRequest("Ada")).Text);
app.Stop();

var fixedModules = app.Start(new LocalHost { ModuleProvider = new FixedModules(new GreetingModule()) });
Console.WriteLine(fixedModules.Send(new GreetRequest("Grace")).Text);
app.Stop();
