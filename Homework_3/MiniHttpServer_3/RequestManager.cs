using MiniHttpServer_3;
using System.Net;
using System.Text;

namespace MiniHttpServer
{
    internal static class RequestManager
    {
        private static readonly SettingsModel _settings = SettingsModel.Instance;

        public static async Task ManageRequest(HttpListenerResponse response, string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    await SendErrorPage(response, 404, "404.html");
                    return;
                }

                string contentType = ContentTypes.GetContentType(filePath);
                response.ContentType = contentType;

                byte[] buffer = await ReadFileContent(filePath, contentType);
                response.ContentLength64 = buffer.Length;

                using Stream output = response.OutputStream;
                await output.WriteAsync(buffer);
                await output.FlushAsync();

                Console.WriteLine($"Файл отправлен: {Path.GetFileName(filePath)} ({contentType})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки запроса файла: {ex.Message}");
                await SendErrorPage(response, 500, "500.html");
            }
        }

        private static async Task<byte[]> ReadFileContent(string filePath, string contentType)
        {
            if (ContentTypes.IsTextContent(contentType))
            {
                string textContent = await File.ReadAllTextAsync(filePath);
                return Encoding.UTF8.GetBytes(textContent);
            }
            else
            {
                return await File.ReadAllBytesAsync(filePath);
            }
        }

        private static async Task SendErrorPage(HttpListenerResponse response, int statusCode, string errorFileName)
        {
            response.StatusCode = statusCode;

            string errorFilePath = Path.Combine(_settings.StaticDirectoryPath, errorFileName);

            if (File.Exists(errorFilePath))
            {
                string errorHtml = await File.ReadAllTextAsync(errorFilePath);
                byte[] buffer = Encoding.UTF8.GetBytes(errorHtml);
                response.ContentLength64 = buffer.Length;
                response.ContentType = "text/html";

                using Stream output = response.OutputStream;
                await output.WriteAsync(buffer);
            }
            else
            {
                await SendDefaultError(response, statusCode);
            }
        }

        private static async Task SendDefaultError(HttpListenerResponse response, int statusCode)
        {
            string errorMessage = statusCode switch
            {
                404 => "404 - Страница не найдена",
                500 => "500 - Внутренняя ошибка сервера",
                _ => "Ошибка"
            };

            byte[] buffer = Encoding.UTF8.GetBytes(errorMessage);
            response.ContentLength64 = buffer.Length;
            response.ContentType = "text/html";

            using Stream output = response.OutputStream;
            await output.WriteAsync(buffer);
        }
    }
}