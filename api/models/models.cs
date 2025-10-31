using Newtonsoft.Json;

namespace api.Models
{
   public class Transaction
    {
        [JsonProperty(PropertyName = "id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [JsonProperty(PropertyName = "tenantId")]
        public string TenantId { get; set; }

        [JsonProperty(PropertyName = "companyId")]
        public string CompanyId { get; set; }

        [JsonProperty(PropertyName = "accountId")]
        public string AccountId { get; set; }
        public string Type { get; set; }
        public string SourceSystem { get; set; }
        public string SourceId { get; set; }
        public DateTime PostedDate { get; set; }
        public DateTime ValueDate { get; set; }
        public double Amount { get; set; }
        public double TaxAmount { get; set; }
        public double NetAmount { get; set; }
        public double TaxRate { get; set; }
        public int PaymentTerms { get; set; }
        public string Currency { get; set; }
        public double FxRate { get; set; }
        public double ConvertedAmount { get; set; }
        public string Category { get; set; }
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