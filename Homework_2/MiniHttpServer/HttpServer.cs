using System.Net;
using System.Text;
using System.Text.Json;

namespace MiniHttpServer
{
    internal class HttpServer
    {
        private HttpListener _listener;
        private bool _isRunning;
        private readonly int _port;

        public HttpServer()
        {
            _listener = new HttpListener();
        }

        public async Task StartServer()
        {
            try
            {
                string settings = File.ReadAllText("settings.json");
                SettingsModel settingsModel = JsonSerializer.Deserialize<SettingsModel>(settings);

                _listener.Prefixes.Add("http://" + settingsModel.Domain + ":" + settingsModel.Port + "/");
                _listener.Start();
                _isRunning = true;

                Console.WriteLine("Server is started");

                _ = Task.Run(ListenForConsoleCommands);

                while (_isRunning)
                {
                    Console.WriteLine("Server is awaiting for request");


                    var context = await _listener.GetContextAsync();
                    var response = context.Response;
                    try
                    {
                        string responseText = File.ReadAllText(settingsModel.StaticDirectoryPath + "index.html");
                        byte[] buffer = Encoding.UTF8.GetBytes(responseText);
                        response.ContentLength64 = buffer.Length;
                        using Stream output = response.OutputStream;
                        await output.WriteAsync(buffer);
                        await output.FlushAsync();

                        Console.WriteLine("Запрос обработан");
                    }
                    catch (DirectoryNotFoundException e)
                    {
                        Console.WriteLine("static folder not found");
                        _listener.Stop();
                        Console.WriteLine("Server is stopped");
                        break;
                    }
                    catch (FileNotFoundException e)
                    {
                        Console.WriteLine("index.html is not found in static folder");
                        _listener.Stop();
                        Console.WriteLine("Server is stopped");
                        break;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("There is an exception: " + e.Message);
                        _listener.Stop();
                        Console.WriteLine("Server is stopped");
                        break;
                    }
                }

            }
            catch (Exception e) when(e is DirectoryNotFoundException or FileNotFoundException)
            {
                Console.WriteLine("settings are not found");
            }
            catch (JsonException e)
            {
                Console.WriteLine("settings.json is incorrect");
            }
            catch (Exception e) 
            { 
                Console.WriteLine("There is an exception: " + e.Message); 
            }
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
