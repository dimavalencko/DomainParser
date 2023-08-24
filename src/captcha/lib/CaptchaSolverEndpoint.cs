namespace DomainsParser;

using System.Net;
using System.Text.Json;

public class CaptchaSolverEndpoint
{
    private Client Client {get; set;}

    public CaptchaSolverEndpoint(Client client) => Client = client;

    public void Bad(string requestId)
    {
        Client.HttpClient.GetAsync(Client.BasePath + "/res.php?key=" + Client.APIKey + "&action=reportbad&id=" + requestId);
    }

    public CaptchaSolvedResponse? GetResponse(string requestId)
    {
        try {
            HttpResponseMessage response = Client.HttpClient.GetAsync(Client.BasePath + "/res.php?key=" + Client.APIKey + "&action=get&id=" + requestId + "&json=1").Result;

            if (response.StatusCode != HttpStatusCode.OK) {
                return null;
            }

            string data = response.Content.ReadAsStringAsync().Result;
            CaptchaSolvedResponse? captchaResponse = JsonSerializer.Deserialize<CaptchaSolvedResponse>(data);
            if (captchaResponse == null) {
                return null;
            }
            
            return captchaResponse;
        } catch (HttpRequestException) {
            return null;
        }        
    }

    public string? SolveRequest(string base64Image)
    {
        MultipartFormDataContent fd = new MultipartFormDataContent();
        fd.Add(new StringContent("base64"), "method");
        fd.Add(new StringContent(Client.APIKey), "key");
        fd.Add(new StringContent(base64Image), "body");
        fd.Add(new StringContent("1"), "json");

        try {
            HttpResponseMessage response = Client.HttpClient.Send(new HttpRequestMessage(){
                Method = HttpMethod.Post,
                Content = fd,
                RequestUri = new Uri(Client.BasePath + "/in.php")
            });

            if (response.StatusCode != HttpStatusCode.OK) {
                return null;
            }

            string data = response.Content.ReadAsStringAsync().Result;
            CaptchaSolveResponse? captchaResponse = JsonSerializer.Deserialize<CaptchaSolveResponse>(response.Content.ReadAsStringAsync().Result);
            if (captchaResponse == null || captchaResponse.request == null) {
                return null;
            }
            
            return captchaResponse.request.ToString();
        } catch (HttpRequestException) {
            return null;
        }
    }
}