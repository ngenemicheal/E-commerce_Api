namespace ECommerce.Application.Exceptions;

public class UnauthorizedException : AppException
{
    public UnauthorizedException() : base("Authentication is required to access this resource.")
    {
    }

    public UnauthorizedException(string message) : base(message)
    {
    }
}
