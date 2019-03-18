using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json.Linq;

namespace Test
{
    class Program
    {
        static HttpListener listener = new HttpListener();

        public static void Main ()
        {
            // Create listener thingy.
            listener.Prefixes.Add("http://localhost/");
            listener.Prefixes.Add("http://192.168.178.9/");
            listener.Start();
            Console.WriteLine("Listening...");
            MainLoop();
        }

        static async void MainLoop ()
        {
            // Main loop
            while (true)
            {
                // Wait for request 
                HttpListenerContext context = listener.GetContext();
                Console.WriteLine("Received a request!");

                // Get request
                HttpListenerRequest request = context.Request;

                //Check if content type is application/json. Send a 415 UnsupportedMediaType if it isn't.
                if (request.ContentType != "application/json")
                {
                    Console.WriteLine("Request has invalid content type. Sending error response and ignoring!");
                    await sendHTMLError(context, "If at first you don't succeed, fail 5 more times.", HttpStatusCode.UnsupportedMediaType);
                    continue;
                }

                System.IO.Stream body = request.InputStream;
                System.Text.Encoding encoding = request.ContentEncoding;
                System.IO.StreamReader reader = new System.IO.StreamReader(body, encoding);
                dynamic requestContent = JObject.Parse(reader.ReadToEnd());
                Console.WriteLine(requestContent.hello);

                // Create response
                HttpListenerResponse response = context.Response;
                response.ContentType = "application/json";
                JObject json = new JObject {
                    { "goodbye", "world" }
                };
                await sendMessage(context, json.ToString(), HttpStatusCode.OK);
            }
        }

        static Task sendHTMLError (HttpListenerContext context, string message, HttpStatusCode statusCode)
        {
            //Send error page
            string responseString = "<HTML><BODY><H1>"+(int)statusCode+" "+statusCode+"</H1>"+message+"</BODY></HTML>";
            sendMessage(context, responseString, statusCode);
            return Task.CompletedTask;
        }

        static Task sendMessage (HttpListenerContext context, string message, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            HttpListenerResponse response = context.Response;
            response.StatusCode = (int) statusCode;
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message);
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();
            return Task.CompletedTask;
        }
    }
}
