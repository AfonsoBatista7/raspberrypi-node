using System.Net;

namespace Centralized {
    public class HttpServer {

        public int Port = 8080;
        public event EventHandler<IotEventData> OnVirtualStateChange = delegate { };

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
                var response = context.Response;

                // do something with the request
                Console.WriteLine($"From: {request.Url}");

                if (request.HasEntityBody) { response.StatusCode = (int)HttpStatusCode.NotAcceptable; }
                else {

                    Console.WriteLine("AAAAAAAAAAAAAAAA");

                    var body = request.InputStream;
                    var encoding = request.ContentEncoding;
                    var reader = new StreamReader(body, encoding);

                    var iotEventData = IotEventData.StringToData(reader.ReadToEnd());

                    if (iotEventData==null) { response.StatusCode = (int)HttpStatusCode.NotAcceptable; }
                    else {
                        //Trigger Gpio Event
                        OnVirtualStateChange?.Invoke(null, iotEventData);

                        response.StatusCode = (int)HttpStatusCode.OK;
                        response.ContentType = "text/plain";
                        response.OutputStream.Write(new byte[] { }, 0, 0);
                    }

                    reader.Close();
                    body.Close();
                } 

                response.OutputStream.Close();

                Receive();
            }
        }
    }
}
