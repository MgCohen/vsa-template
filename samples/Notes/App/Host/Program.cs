using VSA.App;
using VSA.Host;
using VSA.Views;

var builder = WebApplication.CreateBuilder(args);
builder.AddVsa();
builder.AddVsaViews();   // only when the views run in this process

var app = builder.Build();
app.MapVsaRequests();
app.MapVsaViews<App.Views.App>();  // only when the views run in this process
app.Run();
