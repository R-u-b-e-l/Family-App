namespace FamilyManagement.Application.Common.Exceptions;

/// <summary>Base for exceptions whose message is safe to show to the API caller.</summary>
public abstract class AppException(string message) : Exception(message);

public class NotFoundException(string message = "The requested resource was not found.") : AppException(message);

public class BadRequestException(string message, IEnumerable<string>? errors = null) : AppException(message)
{
    public IReadOnlyList<string> Errors { get; } = errors?.ToList() ?? [];
}

public class ForbiddenException(string message = "You do not have permission to perform this action.") : AppException(message);

public class ConflictException(string message) : AppException(message);

public class UnauthorizedException(string message = "Invalid credentials.") : AppException(message);
