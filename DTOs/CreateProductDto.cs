// Data Transfer Object for creating a new product via POST requests.
// Contains only the fields that the client should provide (Id is server-generated).
namespace MyApi.DTOs;

public record CreateProductDto(string Name, decimal Price);
