using MiniHttpServer_3;
using System.Net;
using System.Runtime;
using System.Text;
using System.Text.Json;

namespace MiniHttpServer
{
    internal class HttpServer
    {
        private HttpListener _listener;
        private bool _isRunning;
        private readonly SettingsModel _settings;

        public HttpServer()
        {
            _listener = new HttpListener();
            _settings = SettingsModel.Instance;
        }

        public async Task StartServer()
        {
            try
            {
                string prefix = $"http://{_settings.Domain}:{_settings.Port}/";
                _listener.Prefixes.Add(prefix);
                _listener.Start();
                _isRunning = true;

                Console.WriteLine($"Сервер запущен на {prefix}");
                Console.WriteLine($"Статическая директория: {_settings.StaticDirectoryPath}");

                _ = Task.Run(ListenForConsoleCommands);

                await ProcessRequests();
            }
            catch (Exception e) when (e is DirectoryNotFoundException or FileNotFoundException)
            {
                Console.WriteLine("Файл настроек не найден");
            }
            catch (JsonException e)
            {
                Console.WriteLine("settings.json содержит ошибки");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Произошло исключение: {e.Message}");
            }
        }

        private async Task ProcessRequests()
        {
            while (_isRunning)
            {
                try
                {
                    Console.WriteLine("Ожидание запросов...");
                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(() => ProcessRequest(context));
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка принятия запроса: {ex.Message}");
                }
            }
        }

        private async Task ProcessRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                string filePath = GetFilePath(request.Url!.LocalPath);
                await RequestManager.ManageRequest(response, filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки запроса: {ex.Message}");
                response.StatusCode = 500;
                response.Close();
            }
        }

        private string GetFilePath(string urlPath)
        {
            string cleanPath = urlPath.Trim('/');
            if (string.IsNullOrEmpty(cleanPath))
                cleanPath = "index.html";

            return Path.Combine(_settings.StaticDirectoryPath, cleanPath);
        }

        private async Task ListenForConsoleCommands()
        {
            while (_isRunning)
            {
                try
                {
                    string? input = await Task.Run(() => Console.ReadLine());
                    if (input?.ToLower() == "stop")
                    {
                        Console.WriteLine("Stop command received from console");
                        Stop();
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading console input: {ex.Message}");
                }
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener.Stop();
            _listener.Close();

            Console.WriteLine("Сервер прекратил работу");
        }
    }
}
