using InvoiceStatusProcessorServer;
using System;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    private const string ServerUrl = "http://localhost:8080/";

    static async Task Main(string[] args)
    {

        var configManager = new ConfigurationManager();

        var repository = new InvoiceRepository(configManager);

        var processor = new InvoiceProcessor(configManager, repository);

        var httpServer = new HttpServer(ServerUrl, configManager, processor);
        httpServer.Start();

        await Task.Delay(Timeout.Infinite);

        httpServer.Stop();
    }
}