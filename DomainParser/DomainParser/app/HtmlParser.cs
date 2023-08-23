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
        protected string FilePath = "domains.txt";

        static HttpClient client = new HttpClient();

        Client toCaptcha = new Client("http://2captcha.com", "88a0ea518dce88ec8c93df852a212f49");

        public HtmlParser(){}

        /// <summary>
        /// Главный метод парсинга
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Boolean GetDomainList()
        {
            try
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

                for (int i = 0; i < 1668; i++)
                {
                    Console.WriteLine("Текущая страница - " + pageNum.ToString());
                    var page = GetPageData(uri).Result;
                    var domains = GetAllDomains(page);
                    pageNum++;
                    uri = new Uri("https://statonline.ru/domains?create_to=&tld=ru&page="
                        + pageNum
                        + "&till_from=&sort_field=domain_name_idn&order=ASC&registered=REGISTERED&till_to=&search=&regfilter=123&create_from=&rows_per_page=200&owner=");
                    WriteListInFile(domains); // Записываем результат в файл
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetDomainList. Error body: ", ex.Message);
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Парсим страницу
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<string> GetPageData(Uri uri)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetPageData. Error body: ", ex.Message);
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Установка кук для запроса
        /// </summary>
        /// <param name="fix"></param>
        /// <exception cref="Exception"></exception>
        public void SetCookie(CaptchaFix fix)
        {
            try
            {

                client.DefaultRequestHeaders.Clear(); // Предварительно чистим куки
                client.DefaultRequestHeaders.Add("cookie", $"XSAE={fix.XSAE}; sess_id_={fix.SESS_ID}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in SetCookie. Error body: ", ex.Message);
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Получить список всех доменов
        /// </summary>
        /// <returns>Список всех доменов</returns>
        public List<string> GetAllDomains(string data)
        {
            try
            {
                List<string> allDomainsList = new List<string>();
                var parsedDomains = Regex.Matches(data, "[a-zA-Z0-9][a-zA-Z0-9-]{0,61}[a-zA-Z0-9].RU");

                foreach (Match domain in parsedDomains)
                    allDomainsList.Add(domain.Value);

                return allDomainsList.Distinct().ToList(); // Чистим дубликаты
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetAllDomains. Error body: ", ex.Message);
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Пишем в файл список доменов
        /// </summary>
        /// <param name="list"></param>
        /// <exception cref="Exception"></exception>
        private void WriteListInFile(List<string> list)
        {
           try
            {
                using (FileStream fs = new FileStream(FilePath, FileMode.OpenOrCreate))
                {
                    var result = String.Join(Environment.NewLine, list);
                    result += "\r\n"; // Добавляем отступ в конце строки

                    byte[] input = Encoding.Default.GetBytes(result);

                    fs.Seek(0, SeekOrigin.End); // Переходим в конеч файла и начинаем писать
                    fs.Write(input, 0, input.Length);
                    Console.WriteLine("Записываем в файл данные. Количество доменов - " + list.Count);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in WriteListInFile. Error body: ", ex.Message);
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Читаем файл с доменами
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private List<string> ReadFileToList()
        {
            try
            {
                var list = File.ReadLines("domains.txt").ToList();
                return list;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in ReadFileToList. Error body: ", ex.Message);
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Решаем капчу с помощью библиотеки
        /// </summary>
        /// <returns></returns>
        private CaptchaFix CaptchaSolve()
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetAllDomains. Error body: ", ex.Message);
                throw new Exception(ex.Message);
            }
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
