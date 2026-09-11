namespace ECommerce.Application.Exceptions;

public class ForbiddenException : AppException
{
    public ForbiddenException()
        : base("Access to the requested resource is forbidden.")
    {
    }

    public ForbiddenException(string message) : base(message)
    {
    }
}
