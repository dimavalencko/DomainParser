namespace DomainsParser;

using System.Net.Http;

public class Client
{   
    public string BasePath { get; private set; }

    public string APIKey { get; private set; }

    public HttpClient HttpClient {get; private set;}

    public Client(string basePath, string APIKey)
    {
        this.BasePath = basePath;
        this.APIKey = APIKey;
        this.HttpClient = new HttpClient() {
            BaseAddress = new Uri(this.BasePath)
        };
    }
}
