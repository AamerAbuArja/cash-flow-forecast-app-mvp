using Newtonsoft.Json;

namespace api.Models
{
   public class Transaction
    {
        [JsonProperty(PropertyName = "id")]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [JsonProperty(PropertyName = "projectId")]
        public required string ProjectId { get; set; }
        public required string Type { get; set; }  // "income" or "expense"
        public required string Category { get; set; }
        public required string Description { get; set; }
        public decimal Amount { get; set; }
        public required string Currency { get; set; }
        public decimal FxRate { get; set; }
        public DateTime DateIssued { get; set; }
        public int PaymentTerms { get; set; } // days
        public DateTime PaymentDate { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal ConvertedAmount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
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