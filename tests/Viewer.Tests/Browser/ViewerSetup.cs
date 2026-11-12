namespace Viewer.Tests.Browser;

[SetUpFixture]
public sealed class ViewerSetup
{
    private static ViewerApp? app;

    public static ViewerApp App => app ?? throw new InvalidOperationException("The viewer did not start.");

    [OneTimeSetUp]
    public async Task StartViewer()
    {
        app = await ViewerApp.Start();
    }

    [OneTimeTearDown]
    public void StopViewer()
    {
        app?.Dispose();
        app = null;
    }
}
