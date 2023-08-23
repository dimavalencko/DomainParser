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
            try
            {
                var parser = new HtmlParser();
                var resultPages = parser.GetDomainList();
                Console.WriteLine("Programm ended. Result: " + resultPages);
            }
            catch (Exception ex) {
                Console.WriteLine("Ошибка в файле programm.cs. Тело ошибки: ", ex.Message);
            }
        }
    }
}
