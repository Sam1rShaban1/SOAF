using System.ComponentModel.DataAnnotations;

namespace MyApi.DTOs;

public class PaginationParams
{
    [Required]
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Required]
    [Range(1, 100)]
    public int PageSize { get; set; } = 10;
}
