using Asp.Versioning;
using FamilyManagement.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyManagement.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected static ApiResponse<T> Success<T>(T data, string? message = null) => ApiResponse<T>.Ok(data, message);
    protected static ApiResponse Success(string? message = null) => ApiResponse.Ok(message);
}
