using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace InvoiceStatusProcessorServer
{
    public class InvoiceRepository
    {
        private readonly ConfigurationManager _configManager;

        public InvoiceRepository(ConfigurationManager configManager)
        {
            _configManager = configManager;
        }

        public async Task<List<Invoice>> GetPendingInvoicesAsync()
        {
            var invoices = new List<Invoice>();
            string sql = @"
            SELECT 
                Id, BankName, Amount, Status, UpdatedAt, RetryCount, LastAttemptAt
            FROM 
                Invoices
            WHERE 
                Status = @PendingStatus OR (Status = @ErrorStatus AND RetryCount < @MaxRetries)
            ORDER BY 
                Id;";

            using (var connection = new NpgsqlConnection(_configManager.CurrentConfig.ConnectionString))
            {
                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@PendingStatus", InvoiceStatus.pending.ToString());
                    command.Parameters.AddWithValue("@ErrorStatus", InvoiceStatus.error.ToString());
                    command.Parameters.AddWithValue("@MaxRetries", _configManager.CurrentConfig.MaxErrorRetries);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            invoices.Add(new Invoice
                            {
                                Id = reader.GetInt32(0),
                                BankName = reader.GetString(1),
                                Amount = reader.GetDecimal(2),
                                Status = (InvoiceStatus)Enum.Parse(typeof(InvoiceStatus), reader.GetString(3), true),
                                UpdatedAt = reader.GetDateTime(4),
                                RetryCount = reader.GetInt32(5),
                                LastAttemptAt = reader.GetDateTime(6)
                            });
                        }
                    }
                }
            }
            return invoices;
        }

        public async Task UpdateInvoiceStatusAsync(int invoiceId, InvoiceStatus newStatus, bool incrementRetry)
        {
            string sql = @"
            UPDATE 
                Invoices
            SET 
                Status = @Status, 
                UpdatedAt = @UpdatedAt, 
                RetryCount = RetryCount + @Increment,
                LastAttemptAt = @LastAttemptAt
            WHERE 
                Id = @Id;";

            using (var connection = new NpgsqlConnection(_configManager.CurrentConfig.ConnectionString))
            {
                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Id", invoiceId);
                    command.Parameters.AddWithValue("@Status", newStatus.ToString());
                    command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@LastAttemptAt", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@Increment", incrementRetry ? 1 : 0);

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
