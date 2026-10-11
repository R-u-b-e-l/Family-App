using FamilyManagement.Domain.Entities;
using FamilyManagement.Shared.Dtos;

namespace FamilyManagement.Infrastructure.Identity;

public static class UserDtoMapper
{
    public static UserDto Map(AppUser user, List<string> roles, string familyName) => new()
    {
        Id = user.Id,
        FamilyId = user.FamilyId,
        UserName = user.UserName ?? "",
        Email = user.Email,
        Mobile = user.PhoneNumber,
        FullName = user.FullName,
        DateOfBirth = user.DateOfBirth,
        Gender = user.Gender,
        Address = user.Address,
        ProfilePictureUrl = user.ProfilePicturePath,
        Status = user.Status,
        FamilyName = familyName,
        Roles = roles,
        LastLoginDate = user.LastLoginDate,
        CreatedDate = user.CreatedDate
    };
}
