using FluentValidation;
using FamilyManagement.Shared.Dtos;

namespace FamilyManagement.Application.Validators;

public class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    public CreateAccountRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code).Matches("^[0-9A-Za-z-]{1,20}$").When(x => !string.IsNullOrWhiteSpace(x.Code))
            .WithMessage("Code may contain up to 20 letters, digits or dashes.");
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.AccountType).NotNull().When(x => x.ParentAccountId is null)
            .WithMessage("Account type is required for root accounts.");
    }
}

public class UpdateAccountRequestValidator : AbstractValidator<UpdateAccountRequest>
{
    public UpdateAccountRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class CreatePeriodRequestValidator : AbstractValidator<CreatePeriodRequest>
{
    public CreatePeriodRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate)
            .WithMessage("End date must be after start date.");
    }
}

public class SaveVoucherRequestValidator : AbstractValidator<SaveVoucherRequest>
{
    public SaveVoucherRequestValidator()
    {
        RuleFor(x => x.VoucherType).IsInEnum();
        RuleFor(x => x.Narration).MaximumLength(500);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least two voucher lines are required.")
            .Must(l => l.Count >= 2).WithMessage("At least two voucher lines are required.")
            .Must(l => l.Count <= 100).WithMessage("A voucher may contain at most 100 lines.");
        RuleForEach(x => x.Lines).SetValidator(new SaveVoucherLineRequestValidator());
        RuleFor(x => x)
            .Must(x => decimal.Round(x.Lines.Sum(l => l.Debit), 2) == decimal.Round(x.Lines.Sum(l => l.Credit), 2))
            .WithMessage("Total debit must equal total credit.")
            .When(x => x.Lines.Count > 0);
    }
}

public class SaveVoucherLineRequestValidator : AbstractValidator<SaveVoucherLineRequest>
{
    public SaveVoucherLineRequestValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Debit).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Credit).GreaterThanOrEqualTo(0);
        RuleFor(x => x).Must(l => (l.Debit > 0) ^ (l.Credit > 0))
            .WithMessage("Each line must have either a debit or a credit amount, not both.");
        RuleFor(x => x.Narration).MaximumLength(500);
    }
}
