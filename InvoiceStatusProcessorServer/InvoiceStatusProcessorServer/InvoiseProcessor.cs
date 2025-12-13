using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace InvoiceStatusProcessorServer
{
    public class ProcessingStats
    {
        public int TotalProcessed { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
    }

    public class InvoiceProcessor
    {
        private readonly ConfigurationManager _configManager;
        private readonly InvoiceRepository _repository;
        private Timer? _timer;
        private Random _random = new Random();
        private ProcessingStats _lastStats = new ProcessingStats();

        public ProcessingStats LastStats => _lastStats;

        public InvoiceProcessor(ConfigurationManager configManager, InvoiceRepository repository)
        {
            _configManager = configManager;
            _repository = repository;
            _configManager.ReloadConfig();
            _configManager.OnConfigReloaded += StartProcessing;
            StartProcessing();
        }

        private void StartProcessing()
        {
            int intervalMs = _configManager.CurrentConfig.ProcessingIntervalSeconds * 1000;

            if (_timer == null)
            {
                _timer = new Timer(async _ => await ProcessInvoicesAsync(), null,
                    intervalMs, intervalMs);
            }
            else
            {
                _timer.Change(intervalMs, intervalMs);
            }

        }

        private async Task ProcessInvoicesAsync()
        {
            var invoices = await _repository.GetPendingInvoicesAsync();
            _lastStats = new ProcessingStats();

            foreach (var invoice in invoices)
            {
                await ProcessSingleInvoiceAsync(invoice);
                _lastStats.TotalProcessed++;
            }

            Console.WriteLine($"Обработано: {_lastStats.TotalProcessed} (Успех: {_lastStats.SuccessCount}, Ошибка: {_lastStats.ErrorCount})");
        }

        private async Task ProcessSingleInvoiceAsync(Invoice invoice)
        {
            if (invoice.Status == InvoiceStatus.error && invoice.RetryCount >= _configManager.CurrentConfig.MaxErrorRetries)
            {
                Console.WriteLine($"Достигнуто MaxErrorRetries ({invoice.RetryCount}).");
                return;
            }

            var roll = _random.Next(100);
            InvoiceStatus newStatus = roll < 30 ? InvoiceStatus.success : InvoiceStatus.error;

            bool isRetry = invoice.Status == InvoiceStatus.error;
            bool incrementRetry = false;

            if (newStatus == InvoiceStatus.error)
            {
                if (isRetry || invoice.Status == InvoiceStatus.pending)
                {
                    incrementRetry = true;
                    _lastStats.ErrorCount++;
                }
            }
            else if (newStatus == InvoiceStatus.success)
            {
                incrementRetry = false;
                _lastStats.SuccessCount++;
            }

            if (newStatus != invoice.Status || (newStatus == InvoiceStatus.error && incrementRetry))
            {
                string logMsg = $"[Invoice #{invoice.Id}]: {invoice.Status} -> {newStatus}. ";
                if (isRetry)
                {
                    logMsg += $"(Попытка {invoice.RetryCount + 1}/{_configManager.CurrentConfig.MaxErrorRetries})";
                }
                Console.WriteLine(logMsg);

                await _repository.UpdateInvoiceStatusAsync(invoice.Id, newStatus, incrementRetry);
            }
            else
            {
            }
        }
    }
}
