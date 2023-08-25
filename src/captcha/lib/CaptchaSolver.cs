using System.Net;

namespace DomainsParser;

public class CaptchaSolver
{
    public CaptchaSolverEndpoint Endpoint { get; set; }

    public HttpClient HttpClient{ get; set; }

    private CookieContainer Cookies {get; set;} = new CookieContainer();

    public CaptchaSolver(Client client)
    {
        Endpoint = new CaptchaSolverEndpoint(client);
        HttpClient = new HttpClient(new HttpClientHandler(){
            AllowAutoRedirect = false,
            CookieContainer = Cookies
        });
    }

    public void bad(string requestId)
    {
        Endpoint.Bad(requestId);
    }

    public CaptchaFix trySolveAsync()
    {
        CaptchaFix fix = new CaptchaFix();
        HttpResponseMessage response = HttpClient.SendAsync(new HttpRequestMessage(){
            Method = HttpMethod.Get,
            RequestUri = new Uri("https://statonline.ru/captcha")
        }).Result;

        fix.XSAE = parseCookie(response.Headers.Where(x => x.Key == "Set-Cookie").First().Value.First());

        Cookies.Add(new Uri("https://statonline.ru"), new Cookie("XSAE", fix.XSAE));

        HttpResponseMessage captcha = HttpClient.SendAsync(new HttpRequestMessage(){
            Method = HttpMethod.Get,
            RequestUri = new Uri("https://statonline.ru/generate_captcha"),
        }).Result;

        fix.SESS_ID = parseCookie(captcha.Headers.Where(x => x.Key == "Set-Cookie").First().Value.First());

        string base64 = Convert.ToBase64String(captcha.Content.ReadAsByteArrayAsync().Result);
        Cookies.Add(new Uri("https://statonline.ru"), new Cookie("sess_id_", fix.SESS_ID));

        string? idRes = Endpoint.SolveRequest(base64);
        if (idRes == null) {
            return null;
        }

        Console.WriteLine(idRes);

        fix.RequestId = idRes;
        CaptchaSolvedResponse solvedCaptcha = new CaptchaSolvedResponse();
        do {
            solvedCaptcha = Endpoint.GetResponse(idRes);
            Console.WriteLine("СТАТУС: " + solvedCaptcha.request + ". ОТВЕТ: " + solvedCaptcha.status);
            if (solvedCaptcha == null) {
                Console.WriteLine("Произошла ошибка в HTTP запросе к капче");
                break;
            }

            if ((solvedCaptcha.status == 0 && solvedCaptcha.request != "CAPCHA_NOT_READY") || (solvedCaptcha.status == 1 && solvedCaptcha.request == "ERROR_BAD_DUPLICATES") || (solvedCaptcha.status == 0 && solvedCaptcha.request == "ERROR_BAD_DUPLICATES")) {
                Console.WriteLine("Не удалось решить капчу");
                return null;
            }

            Thread.Sleep(2500);
        } while (solvedCaptcha.status == 0);

        if (solvedCaptcha == null) {
            return null;
        }

        fix.Response = solvedCaptcha.request.ToUpper();

        MultipartFormDataContent solveCaptchaFd = new MultipartFormDataContent();
        solveCaptchaFd.Add(new StringContent(fix.Response), "captcha");

        HttpResponseMessage solveCaptcha = HttpClient.SendAsync(new HttpRequestMessage(){
            Method = HttpMethod.Post,
            RequestUri = new Uri("https://statonline.ru/captcha"),
            Content = solveCaptchaFd
        }).Result;

        Console.WriteLine("CAPTCHA HAS BEEN SOLVED");
        return fix;
    }

    private string parseCookie(string cookieAsText)
    {
        return cookieAsText.Split(';')[0].Split('=')[1];
    }
}
