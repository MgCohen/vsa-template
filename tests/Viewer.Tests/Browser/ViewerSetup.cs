using Microsoft.Playwright;

namespace Viewer.Tests.Browser;

[SetUpFixture]
public sealed class ViewerSetup
{
    private static ViewerApp? app;
    private static IPlaywright? playwright;
    private static IBrowser? browser;

    public static ViewerApp App => app ?? throw new InvalidOperationException("The viewer did not start.");

    public static IBrowser Browser => browser ?? throw new InvalidOperationException("Chromium did not start.");

    [OneTimeSetUp]
    public async Task StartViewer()
    {
        int installed = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.That(installed, Is.Zero, "The Playwright Chromium install failed.");
        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        app = await ViewerApp.Start();
    }

    [OneTimeTearDown]
    public async Task StopViewer()
    {
        app?.Dispose();
        app = null;
        if (browser is not null)
        {
            await browser.CloseAsync();
            browser = null;
        }
        playwright?.Dispose();
        playwright = null;
    }
}
