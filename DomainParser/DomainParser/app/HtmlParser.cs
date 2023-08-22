using DomainsParser;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        public List<string> GetDomainList()
        {
            int pageNum = 1;
            Uri uri = new Uri("https://statonline.ru/domains?create_to=&tld=ru&page="
                + pageNum
                + "&till_from=&sort_field=domain_name_idn&order=ASC&registered=REGISTERED&till_to=&search=&regfilter=123&create_from=&rows_per_page=200&owner=");

            CaptchaSolver solver = new CaptchaSolver(toCaptcha);

            #region Оставил часть кода, т.к все равно в GetPageData будет запрашивать капчу

            CaptchaFix captchaFix = solver.trySolveAsync();
            CookieContainer container = new CookieContainer();

            container.Add(uri, new Cookie("sess_id_", captchaFix.SESS_ID));
            container.Add(uri, new Cookie("XSAE", captchaFix.XSAE));
            client.DefaultRequestHeaders.Add("cookie", container.GetCookieHeader(uri));

            #endregion

            for(int i = 0; i < 20; i++)
            {
                var page = GetPageData(uri).Result;
                GetAllDomains(page);
                pageNum++;
                uri = new Uri("https://statonline.ru/domains?create_to=&tld=ru&page="
                    + pageNum 
                    + "&till_from=&sort_field=domain_name_idn&order=ASC&registered=REGISTERED&till_to=&search=&regfilter=123&create_from=&rows_per_page=200&owner=");
            }

            AllDomains = AllDomains.Distinct().ToList(); // Окончательный раз чистим дубликаты
            WriteListInFile(AllDomains); // Записываем результат в файл
            return AllDomains;
        }

        public async Task<string> GetPageData(Uri uri)
        {
            var stringPage = await client.GetAsync(uri);
            var bytePage = stringPage.Content.ReadAsByteArrayAsync().Result;
            var result = Encoding.UTF8.GetString(bytePage);

            // Проверяем на наличие капчи
            var regResut = Regex.Match(result, @"input:\[name=""captcha""\](\w*)");

            if (regResut.Success) // Если капча
            {
                var fix = CaptchaSolve(); // Решаем 
                SetCookie(fix);
                result = GetPageData(uri).Result; // Снова просим страницу
            }
            return result;
        }

        public void SetCookie(CaptchaFix fix)
        {
            client.DefaultRequestHeaders.Clear(); // Предварительно чистим куки
            client.DefaultRequestHeaders.Add("cookie", $"XSAE={fix.XSAE}; sess_id_={fix.SESS_ID}");
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

            var withoutDuplicates = allDomainsList.Distinct().ToList(); // Чистим дубликаты
            AllDomains = AllDomains.Concat(withoutDuplicates).ToList(); // Объединяем текущий список с существующим
            // По факту Concat() должен чистить дубли, но он этого не делает, как и AddRange()
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
