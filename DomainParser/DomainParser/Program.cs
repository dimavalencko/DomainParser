using DomainParser.app;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainParser
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var parser = new HtmlParser();
            var resultPages = parser.GetDomainList();
        }
    }
}
