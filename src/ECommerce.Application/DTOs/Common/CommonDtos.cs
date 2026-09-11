namespace ECommerce.Application.DTOs.Common;

public record MoneyDto(
    decimal Amount, 
    string Currency);

public record PagedResponse<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize));
}
