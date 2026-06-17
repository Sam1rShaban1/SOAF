using System.ComponentModel.DataAnnotations;

namespace MyApi.DTOs;

public class ProductDto
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public decimal Price { get; set; }
}
