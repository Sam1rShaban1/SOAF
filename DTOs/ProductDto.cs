// Data Transfer Object for product responses.
// Used to decouple the internal data model from the API contract.
// Only exposes fields safe for external consumption.
namespace MyApi.DTOs;

public record ProductDto(int Id, string Name, decimal Price);
