namespace ECommerce.Domain.Exceptions;

public class InvalidStockQuantityException : DomainException
{
    public int Requested { get; }
    public int Available { get; }

    public InvalidStockQuantityException(int requested, int available)
        : base($"Insufficient stock. Requested {requested} but only {available} available.")
    {
        Requested = requested;
        Available = available;
    }
}
