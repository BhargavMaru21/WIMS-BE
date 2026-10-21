using Microsoft.AspNetCore.Http;

namespace WIMS.Application.DTOs.Products;

public class ImportDto
{
    public required IFormFile File { get; set; } 
}
