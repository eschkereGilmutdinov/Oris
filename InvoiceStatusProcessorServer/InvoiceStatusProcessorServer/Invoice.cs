using System;

public enum InvoiceStatus
{
    pending,
    success,
    error
}

public class Invoice
{
    public int Id { get; set; }
    public string BankName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public InvoiceStatus Status { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int RetryCount { get; set; }
    public DateTime LastAttemptAt { get; set; }
}
