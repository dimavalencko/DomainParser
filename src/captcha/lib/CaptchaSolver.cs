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

        string solvedCaptcha = string.Empty;
        int iterator = 0;
        do {
            solvedCaptcha = Endpoint.GetResponse(idRes) ?? string.Empty;
            Console.WriteLine(solvedCaptcha);
            iterator++;
            Thread.Sleep(5000);
        } while ((solvedCaptcha.Length == 0 || solvedCaptcha == "CAPCHA_NOT_READY") && iterator < 8);

        if (solvedCaptcha == null) {
            return null;
        }

        fix.RequestId = solvedCaptcha;

        HttpResponseMessage solveCaptcha = HttpClient.SendAsync(new HttpRequestMessage(){
            Method = HttpMethod.Post,
            RequestUri = new Uri("https://statonline.ru/captcha")
        }).Result;

        if (solveCaptcha.IsSuccessStatusCode) {
            return fix;
        }

        return null;
    }

    private string parseCookie(string cookieAsText)
    {
        return cookieAsText.Split(';')[0].Split('=')[1];
    }
}