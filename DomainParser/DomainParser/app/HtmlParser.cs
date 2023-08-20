using DomainsParser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DomainParser.app
{
    public class HtmlParser
    {
        List<string> AllDomains= new List<string>();
        static HttpClient client = new HttpClient();
        Client toCaptcha = new Client("http://2captcha.com", "88a0ea518dce88ec8c93df852a212f49");

        public HtmlParser()
        {
        }

        public async Task GetDomainList()
        {
            int pageNum = 1;
            Uri uri = new Uri("https://statonline.ru/domains?create_to=&tld=ru&page=" + pageNum + "&till_from=&sort_field=domain_name_idn&order=ASC&registered=REGISTERED&till_to=&search=&regfilter=123&create_from=&rows_per_page=200&owner=");

            CaptchaSolver solver = new CaptchaSolver(toCaptcha);
            CaptchaFix captchaFix = solver.trySolveAsync();

            CookieContainer container = new CookieContainer();

            container.Add(uri, new Cookie("sess_id_", captchaFix.SESS_ID));
            container.Add(uri, new Cookie("XSAE", captchaFix.XSAE));

            client.DefaultRequestHeaders.Add("Cookie", container.GetCookieHeader(uri));
            //Regex captch

            for(int i = 0; i < 1; i++)
            {
                var page = GetPageData(uri).Result;
                var regResut = Regex.Match(page, @"input:\[name=""captcha""\](\w*)");

                // Нужно решить капчу
                if (regResut.Success) CaptchaSolve();

                // Тут нужно получить кол-во страниц и в цикле кидать запросы

                GetAllDomains(page);
            }
        }

        public async Task<string> GetPageData(Uri uri)
        {
            var page = await client.GetAsync(uri);
            var result = page.Content.ReadAsByteArrayAsync().Result;
            var result1 = Encoding.UTF8.GetString(result);
            return result1;
        }

        /// <summary>
        /// Получить список всех доменов
        /// </summary>
        /// <returns>Список всех доменов</returns>
        public void GetAllDomains(string data)
        {
            List<string> allDomainsList = new List<string>();
            var parsedDomains = Regex.Matches(data, "[a-zA-Z0-9][a-zA-Z0-9-]{0,61}[a-zA-Z0-9].RU");

            foreach (Match domain in parsedDomains)
                allDomainsList.Add(domain.Value);

            AllDomains = allDomainsList.Distinct().ToList(); // Чистим дубликаты

            WriteListInFile(AllDomains);
        }

        private async void WriteListInFile(List<string> list)
        {
            File.WriteAllLines("domains.txt", list);
        }

        private List<string> ReadFileToList()
        {
            var list = File.ReadLines("domains.txt").ToList();
            return list;
        }

        private CaptchaFix CaptchaSolve()
        {
            CaptchaSolver solver = new CaptchaSolver(toCaptcha);
            CaptchaFix fix = new CaptchaFix();

            bool isCaptchaSolved = false;
            while (isCaptchaSolved != true)
            {
                fix = solver.trySolveAsync();
                isCaptchaSolved = fix != null;
            }

            return fix;
        }

        /// <summary>
        /// Получить список "брошенных" доменов
        /// </summary>
        /// <returns>Список брошенных доменов</returns>
        //public List<string> GetCastDomains()
        //{

        //}
    }
}
