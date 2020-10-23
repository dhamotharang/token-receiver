using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Token.Models;
using Token.Options;

namespace Token.Services
{
    public class PageFactoryManager : IDisposable
    {
        public PageFactoryManager(IOptions<ApplicationOptions> options)
        {
            _pageFactories.Add(Applications.Matsumoto, new MatsumotoPageFactory(Microsoft.Extensions.Options.Options.Create(options.Value.Store[Applications.Matsumoto])));
            _pageFactories.Add(Applications.Shuri, new ShuriPageFactory(Microsoft.Extensions.Options.Options.Create(options.Value.Store[Applications.Shuri])));
        }

        
        public Task Init()
        {
            return Task.WhenAll(_pageFactories.Select(pg => pg.Value.Init()));
        }


        public PageFactory GetFactory(Applications app) => _pageFactories[app];
        
        
        public void Dispose()
        {
            foreach (var pageFactory in _pageFactories)
            {
                pageFactory.Value.Dispose();
            }
        }
        
        
        private readonly Dictionary<Applications, PageFactory> _pageFactories = new Dictionary<Applications, PageFactory>();
    }
}