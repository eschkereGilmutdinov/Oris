using System;
using System.IO;
using System.Text.Json;
using System.Threading;

public class AppConfig
{
    public int ProcessingIntervalSeconds { get; set; } = 300;
    public int MaxErrorRetries { get; set; } = 5;
    public string ConnectionString { get; set; } = string.Empty;
}

public class ConfigurationManager
{
    private const string ConfigFileName = "config.json";
    private AppConfig _currentConfig = new AppConfig();
    private readonly FileSystemWatcher _watcher;

    public event Action? OnConfigReloaded;

    public AppConfig CurrentConfig => _currentConfig;

    public ConfigurationManager()
    {
        ReloadConfig();

        _watcher = new FileSystemWatcher(Directory.GetCurrentDirectory(), ConfigFileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName
        };

        _watcher.Changed += (sender, e) =>
        {
            Thread.Sleep(500);
            ReloadConfig();
        };

        _watcher.EnableRaisingEvents = true;
        Console.WriteLine($"Запущено отслеживание файла {ConfigFileName}.");
    }

    public void ReloadConfig()
    {
        try
        {
            if (!File.Exists(ConfigFileName))
            {
                Console.WriteLine($"Файл {ConfigFileName} не найден.");
                return;
            }

            var jsonString = File.ReadAllText(ConfigFileName);
            var newConfig = JsonSerializer.Deserialize<AppConfig>(jsonString);

            if (newConfig != null)
            {
                _currentConfig = newConfig;
                Console.WriteLine("Конфигурация успешно перезагружена.");

                OnConfigReloaded?.Invoke();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при чтении конфигурации: {ex.Message}");
        }
    }
}