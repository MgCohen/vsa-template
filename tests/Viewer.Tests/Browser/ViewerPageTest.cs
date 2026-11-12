using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace Viewer.Tests.Browser;

public abstract class ViewerPageTest : PlaywrightTest
{
    private const int AppEdges = 39;

    private IBrowser? browser;
    private IBrowserContext? context;

    protected GraphPage Graph { get; private set; } = null!;

    [SetUp]
    public async Task OpenViewer()
    {
        browser = await LaunchEdge();
        context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 2080, Height = 1200 },
            AcceptDownloads = true,
        });
        await context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"],
            new BrowserContextGrantPermissionsOptions { Origin = ViewerSetup.App.Url });
        IPage page = await context.NewPageAsync();
        await page.GotoAsync(ViewerSetup.App.Url);
        await page.WaitForFunctionAsync("n => document.querySelectorAll('svg.canvas path[marker-end]').length >= n",
            AppEdges, new PageWaitForFunctionOptions { Timeout = 30000 });
        await page.WaitForTimeoutAsync(1500);
        Graph = new GraphPage(page);
    }

    [TearDown]
    public async Task CloseViewer()
    {
        if (context is not null)
        {
            await context.CloseAsync();
        }
        if (browser is not null)
        {
            await browser.CloseAsync();
        }
    }

    private async Task<IBrowser> LaunchEdge()
    {
        try
        {
            return await BrowserType.LaunchAsync(new BrowserTypeLaunchOptions { Channel = "msedge", Headless = true });
        }
        catch (PlaywrightException e) when (e.Message.Contains("msedge", StringComparison.Ordinal))
        {
            Assert.Ignore($"Microsoft Edge is not installed, so the browser tests do not run. {e.Message}");
            throw;
        }
    }
}
