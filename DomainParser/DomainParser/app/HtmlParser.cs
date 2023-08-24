using DomainsParser;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Metrics;
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
        protected string FilePath;

        protected int CaptchaSolveCount = 0;

        protected Uri uri;

        protected static HttpClient client = new HttpClient();

        protected Client toCaptcha;

        protected CaptchaSolver solver;

        public HtmlParser() {
            toCaptcha = new Client("https://2captcha.com", "eafa7b7a0ad4ee02f47bcf463e3a6ebe");
            solver = new CaptchaSolver(toCaptcha);
            FilePath = GenerateFileName();
            uri = new Uri("https://statonline.ru/domains?create_to=&tld=ru&page=1&till_from=&sort_field=domain_name_idn&order=ASC&registered=REGISTERED&till_to=&search=&regfilter=123&create_from=&rows_per_page=200&owner=");
        }

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
                    var parsedPage = ParsePage().Result; // Получаем страницу, которую спарсили
                    var capthaIsSolved = CheckCaptchaOnPage(parsedPage);
                    if(!capthaIsSolved) // Пробуем снова на этой странице
                    {
                        i--;
                        continue;
                    }
                    var domains = GetAllDomains(parsedPage);

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
        /// Ршаем капчу
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public CaptchaFix SolveCaptcha(string page)
        {
            try
            {
                CaptchaSolveCount++; // 1
                CaptchaFix fix = null;

                // Пока капча не решилась - отправляем запросы на решение
                do { 
                    fix = CaptchaSolve();
                    Console.WriteLine("Попытка решить капчу. Ответ: " + fix.Response);
                }
                while (fix == null);
                    
                SetCookie(fix);
                return fix;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetPageData. Error body: ", ex.Message);
                throw new Exception(ex.Message);
            }
        }
         
        /// <summary>
        /// Парсим страницу
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<string> ParsePage()
        {
            try
            {
                var stringPage = await client.GetAsync(uri);
                var bytePage = stringPage.Content.ReadAsByteArrayAsync().Result;
                return Encoding.UTF8.GetString(bytePage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetPageData. Error body: ", ex.Message);
                throw new Exception(ex.Message);
            }
        } /// <summary>
        
        /// Проверяем на наличие капчи
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Boolean CheckCaptchaOnPage(string page)
        {
            int counter = 0;
            string captchaRegex = @"input:\[name=""captcha""\](\w*)";
            try
            {
                var regResut = Regex.Match(page, captchaRegex);

                if(regResut.Success) // Если нужно решить капчу
                {
                    Console.WriteLine("Нужно решить капчу");
                    CaptchaFix fix = null;
                    do
                    {
                        counter++;

                        fix = SolveCaptcha(page); // Решаем капчу и ставим куки
                        var parsedPage = ParsePage().Result; // Снова парсим страницу и проверяем на наличие капчи

                        if (Regex.Match(parsedPage, captchaRegex).Success) // Если снова капча - возврат денег
                        {
                            Console.WriteLine("Капча решена неверно. Возврат денег.");
                            solver.bad(fix.RequestId);
                            fix = null;
                        }
                        else
                        {
                            Console.WriteLine("Капча решена. Попыток - " + counter.ToString());
                            return true;
                        }
                    }
                    while (fix == null);
                }
                return true;
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
        /// Генерируем имя файла
        /// </summary>
        /// <returns>Список брошенных доменов</returns>
        public string GenerateFileName()
        {
            string date = '-' + DateTime.Now.ToShortDateString();
            string time = '-' + DateTime.Now.Hour.ToString() + '.' + DateTime.Now.Minute.ToString() + '.' + DateTime.Now.Second.ToString();
            return "domains" + date + time + ".txt";
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
