using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using MiniHttpServer;

namespace MiniHttpServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var server = new HttpServer();

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                server.Stop();
            };

            try
            {
                await server.StartServer();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Критическая ошибка: {ex.Message}");
            }

            Console.ReadKey();
        }
    }
}