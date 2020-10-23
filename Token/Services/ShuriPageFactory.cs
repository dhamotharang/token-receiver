using Microsoft.Extensions.Options;
namespace Token.Services
{
    public class ShuriPageFactory : PageFactory
    {
        public ShuriPageFactory(IOptions<BaseUrlOptions> options) : base(options)
        { }
    }
}