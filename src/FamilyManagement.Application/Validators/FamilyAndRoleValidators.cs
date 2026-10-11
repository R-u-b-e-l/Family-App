using FluentValidation;
using FamilyManagement.Shared.Authorization;
using FamilyManagement.Shared.Dtos;

namespace FamilyManagement.Application.Validators;

public class UpdateFamilyRequestValidator : AbstractValidator<UpdateFamilyRequest>
{
    public UpdateFamilyRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class UpdateFamilySettingsRequestValidator : AbstractValidator<UpdateFamilySettingsRequest>
{
    public UpdateFamilySettingsRequestValidator()
    {
        RuleFor(x => x.Currency).NotEmpty().Length(3).Matches("^[A-Za-z]{3}$")
            .WithMessage("Currency must be a 3-letter ISO code (e.g. BDT).");
        RuleFor(x => x.FinancialYearStartMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.DateFormat).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Timezone).NotEmpty().MaximumLength(60);
        RuleFor(x => x.ContactEmail).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));
    }
}

public class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleForEach(x => x.PermissionCodes).Must(c => PermissionCodes.All.Contains(c))
            .WithMessage((_, code) => $"Unknown permission code '{code}'.");
    }
}

public class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
    }
}

public class UpdateRolePermissionsRequestValidator : AbstractValidator<UpdateRolePermissionsRequest>
{
    public UpdateRolePermissionsRequestValidator()
    {
        RuleForEach(x => x.PermissionCodes).Must(c => PermissionCodes.All.Contains(c))
            .WithMessage((_, code) => $"Unknown permission code '{code}'.");
    }
}
