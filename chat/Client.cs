using System.Net;
using System.Runtime.ConstrainedExecution;
using System.Text;

 namespace Centralized {

    public class Client {

        public const string DEFAULT_URI = "http://localhost:8080/";

        private readonly HttpClient _client;
        private string _uri;

        public Client() {
            _client = new HttpClient();
            _uri = "";
        }
        public Client(string uri) {
            _client = new HttpClient();
            _uri = uri;
        }

        private async Task SendRequest(IotEventData data) {
            HttpContent contentData = new StringContent(data.DataToString(), Encoding.UTF8, "text/plain");
            HttpResponseMessage resp = await _client.PutAsync(
                _uri=="" ? DEFAULT_URI : _uri,
                contentData);

            string body = await resp.Content.ReadAsStringAsync();

            Console.WriteLine(
                $"status: {resp.StatusCode}, version: {resp.Version}, " +
                $"body: {body[..Math.Min(100, body.Length)]}");
        }

        public async void HandlePhysicalStateChange(IotEventData args) {
            await SendRequest(args);
        }
    }
}
