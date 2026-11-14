using Microsoft.Playwright;

namespace Viewer.Tests.Browser;

public abstract class ViewerPageTest
{
    private const int AppEdges = 1;

    private IBrowserContext? context;

    protected GraphPage Graph { get; private set; } = null!;

    [SetUp]
    public async Task OpenViewer()
    {
        context = await ViewerSetup.Browser.NewContextAsync(new BrowserNewContextOptions
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
            context = null;
        }
    }
}
