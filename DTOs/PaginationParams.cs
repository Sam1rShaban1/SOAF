// Encapsulates pagination request parameters with sensible defaults.
// PageNumber starts at 1, PageSize defaults to 10 items per page.
namespace MyApi.DTOs;

public record PaginationParams(int PageNumber = 1, int PageSize = 10);
