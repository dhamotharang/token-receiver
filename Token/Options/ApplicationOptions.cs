using System.Collections.Generic;
using Token.Models;

namespace Token.Options
{
    public class ApplicationOptions
    {
        public Dictionary<Applications, BaseUrlOptions> Store { get; } = new Dictionary<Applications, BaseUrlOptions>();
    }
}