using System.Net;

public class HttpServer
{
    public int Port = 8080;

    private HttpListener? _listener;

    public void Start() {
        _listener = new HttpListener();
        _listener.Prefixes.Add("http://*:" + Port.ToString() + "/");
        _listener.Start();
        Receive();
    }

    public void Stop() {
        _listener?.Stop();
    }

    private void Receive() {
        _listener?.BeginGetContext(new AsyncCallback(ListenerCallback), _listener);
    }

    private void ListenerCallback(IAsyncResult result) {

        if (_listener!=null && _listener.IsListening) {
            var context = _listener.EndGetContext(result);
            var request = context.Request;

            // do something with the request
            Console.WriteLine($"{request.Url}");

            Receive();
        }
    }
}
