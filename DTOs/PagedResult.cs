// Generic wrapper for paginated API responses.
// Carries the current page items along with total count and computed page count.
namespace MyApi.DTOs;

public record PagedResult<T>(IEnumerable<T> Items, int TotalCount, int PageNumber, int PageSize)
{
    // Computed property: total number of pages based on total count and page size.
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
