using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using HappyTravel.TokenReceiver.Api.Options;
using Microsoft.Extensions.Options;
using PuppeteerSharp;

namespace HappyTravel.TokenReceiver.Api.Services
{
    public abstract class PageFactory : IDisposable
    {
        protected PageFactory(IOptions<BaseUrlOptions> options)
        {
            _options = options.Value;
            _pageStack = new ConcurrentStack<Page>();
        }


        public async Task Init()
        {
            _browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = new []{"--no-sandbox --disable-setuid-sandbox --incognito --user-data-dir=/tmp/session-123 --disable-dev-shm-usage"},
                ExecutablePath = (await GetRevisionInfo()).ExecutablePath
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


        private static async ValueTask<RevisionInfo> GetRevisionInfo()
            => _revisionInfo ??= await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);

        
        private const int StackLimit = 8;
        
        private static Browser _browser;
        private readonly BaseUrlOptions _options;
        private readonly ConcurrentStack<Page> _pageStack;
        private static RevisionInfo _revisionInfo;
    }
}
