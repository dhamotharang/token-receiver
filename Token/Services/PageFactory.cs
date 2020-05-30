using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using PuppeteerSharp;

namespace Token.Services
{
    public class PageFactory : IDisposable
    {
        public PageFactory(IOptions<BaseUrlOptions> options)
        {
            _options = options.Value;
            _pageStack = new ConcurrentStack<Page>();
        }


        public async Task Init()
        {
            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
            _browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = new []{"--no-sandbox --disable-setuid-sandbox --incognito --user-data-dir=/tmp/session-123"}
            });

            for (var i = 0; i < StackLimit; i++)
            {
                var page = await _browser.NewPageAsync();
                _pageStack.Push(page);
            }
        }


        public void Dispose()
        {
            if (!_browser.IsClosed)
                _browser.Dispose();
        }


        public async Task Dispose(Page page)
        {
            if (page.IsClosed)
                return;

            await page.GoToAsync($"{_options.Application}logout");
            await page.WaitForNavigationAsync();

            if (_pageStack.Count < StackLimit)
                _pageStack.Push(page);
            else
                await page.DisposeAsync();
        }


        public async ValueTask<Page> Get()
        {
            if (_pageStack.TryPop(out var page))
                return page;

            if (_browser.IsClosed)
                await Init();

            return await _browser.NewPageAsync();
        }


        private const int StackLimit = 8;

        private static Browser _browser;
        private readonly BaseUrlOptions _options;
        private readonly ConcurrentStack<Page> _pageStack;
    }
}
