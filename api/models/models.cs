using System.ComponentModel;
using System.Runtime;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace api.Models
{
    public class Transaction : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        public Transaction()
        {
            PropertyChanged += CalculateDependentProperties;
        }

        // Implement INotifyPropertyChanged to handle property changes
        private void CalculateDependentProperties(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Amount) || e.PropertyName == nameof(TaxRate))
            {
                TaxAmount = Amount * TaxRate / 100;
                NetAmount = Amount - TaxAmount;
            }
        }
        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [JsonProperty(PropertyName = "id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Sender-provided transaction id for external reference / idempotency
        [JsonProperty(PropertyName = "senderTransactionId")]
        public required string SenderTransactionId { get; set; }

        [JsonProperty(PropertyName = "tenantId")]
        public required string TenantId { get; set; }

        [JsonProperty(PropertyName = "companyId")]
        public required string CompanyId { get; set; }

        [JsonProperty(PropertyName = "accountId")]
        public required string AccountId { get; set; }

        public required string Type { get; set; }

        public string SourceSystem { get; set; }

        public string SourceId { get; set; }

        public DateTime PostedDate { get; set; }

        public DateTime ValueDate { get; set; }

        public double Amount { get; set; }

        public double TaxRate { get; set; }

        public double TaxAmount { get; set; }

        public double NetAmount { get; set; }

        public int PaymentTerms { get; set; }

        public required string Currency { get; set; }

        public double FxRate { get; set; }

        public double ConvertedAmount { get; set; }

        public required string Category { get; set; }

        public string Description { get; set; }

        public InvoiceDetails Invoice { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime PaymentDate { get; set; }

        public DateTime DateIssued { get; set; }
    }

    public class InvoiceDetails
    {
        public string InvoiceId { get; set; }
        public DateTime DueDate { get; set; }
        public int PaymentTermsDays { get; set; }
        public string Status { get; set; }
    }

    public class ForecastResult
    {
        public required string Period { get; set; }   // "2025-10", "2025-Q4"
        public decimal OpeningBalance { get; set; }
        public decimal Inflows { get; set; }
        public decimal Outflows { get; set; }
        public decimal ClosingBalance { get; set; }
    }
}