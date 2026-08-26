using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisionCheckAI.Server.Data;
using VisionCheckAI.Server.Models;

namespace VisionCheckAI.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly VisionCheckDbContext _db;

    public ProductsController(VisionCheckDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetProducts()
    {
        var products = await _db.Products
            .Where(p => p.IsActive)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Category = p.Category,
                IsActive = p.IsActive
            })
            .ToListAsync();

        return Ok(products);
    }
}
