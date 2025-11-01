using FluentValidation;
using api.Models;
using System;

namespace api.Validators
{
    public class TransactionValidator : AbstractValidator<Transaction>
    {
        public TransactionValidator()
        {
            RuleFor(t => t.TenantId).NotEmpty().WithMessage("tenantId is required.");
            RuleFor(t => t.CompanyId).NotEmpty().WithMessage("companyId is required.");
            RuleFor(t => t.AccountId).NotEmpty().WithMessage("accountId is required.");
            RuleFor(t => t.Amount).GreaterThan(0).WithMessage("amount must be > 0.");
            RuleFor(t => t.Currency).NotEmpty().Length(3).WithMessage("currency must be a 3-letter code.");
            RuleFor(t => t.FxRate).GreaterThan(0).WithMessage("fxRate must be > 0.");
            RuleFor(t => t.NetAmount).GreaterThanOrEqualTo(0).WithMessage("netAmount must be >= 0.");
            RuleFor(t => t.PostedDate)
                .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
                .WithMessage("postedDate can't be in the far future.");
            RuleFor(t => t.ValueDate)
                .NotNull()
                .LessThanOrEqualTo(DateTime.UtcNow.AddDays(30))
                .WithMessage("valueDate too far in the future.");
            // Optional: business rule to validate SenderTransactionId length
            RuleFor(t => t.SenderTransactionId).NotEmpty()
                .WithMessage("senderTransactionId is required.")
                .MaximumLength(200);
        }
    }
}