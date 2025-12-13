using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace InvoiceStatusProcessorServer
{
    public class HttpServer
    {
        private readonly HttpListener _listener;
        private readonly ConfigurationManager _configManager;
        private readonly InvoiceProcessor _processor;
        private readonly string _url;

        public HttpServer(string url, ConfigurationManager configManager, InvoiceProcessor processor)
        {
            _url = url;
            _configManager = configManager;
            _processor = processor;
            _listener = new HttpListener();
            _listener.Prefixes.Add(url);
        }

        public void Start()
        {
            try
            {
                _listener.Start();
                Console.WriteLine($"Сервер слушает на {_url}");
                Task.Run(Listen);
            }
            catch (HttpListenerException ex)
            {
                Console.WriteLine($"Ошибка запуска HttpListener: {ex.Message}");
            }
        }

        public void Stop()
        {
            if (_listener.IsListening)
            {
                _listener.Stop();
                _listener.Close();
                Console.WriteLine("Сервер остановлен.");
            }
        }

        private async Task Listen()
        {
            while (_listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(() => ProcessRequest(context));
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при получении контекста: {ex.Message}");
                }
            }
        }

        private async Task ProcessRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                switch (request.Url?.AbsolutePath.ToLower())
                {
                    case "/health":
                        await HandleHealth(response);
                        break;
                    case "/config":
                        await HandleConfig(response);
                        break;
                    case "/config/reload":
                        await HandleConfigReload(request, response);
                        break;
                    case "/stats":
                        await HandleStats(response);
                        break;
                    default:
                        response.StatusCode = 404;
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке запроса: {ex.Message}");
                response.StatusCode = 500;
            }
            finally
            {
                response.Close();
            }
        }

        private async Task WriteResponse(HttpListenerResponse response, string content, string contentType = "text/plain", int statusCode = 200)
        {
            var buffer = Encoding.UTF8.GetBytes(content);
            response.ContentType = contentType;
            response.ContentLength64 = buffer.Length;
            response.StatusCode = statusCode;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        }

        private Task HandleHealth(HttpListenerResponse response)
        {
            return WriteResponse(response, "OK");
        }

        private Task HandleConfig(HttpListenerResponse response)
        {
            var config = new
            {
                _configManager.CurrentConfig.ProcessingIntervalSeconds,
                _configManager.CurrentConfig.MaxErrorRetries,
                ConnectionString = "(masked)"
            };
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            return WriteResponse(response, json, "application/json");
        }

        private Task HandleConfigReload(HttpListenerRequest request, HttpListenerResponse response)
        {
            if (request.HttpMethod.ToUpper() != "POST")
            {
                response.StatusCode = 405;
                return Task.CompletedTask;
            }

            _configManager.ReloadConfig();


            return WriteResponse(response, "reloaded");
        }

        private Task HandleStats(HttpListenerResponse response)
        {
            var stats = _processor.LastStats;
            var json = JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true });
            return WriteResponse(response, json, "application/json");
        }
    }
}
