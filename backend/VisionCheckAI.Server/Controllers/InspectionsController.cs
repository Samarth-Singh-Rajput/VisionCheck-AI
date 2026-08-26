using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisionCheckAI.Server.Data;
using VisionCheckAI.Server.Data.Entities;
using VisionCheckAI.Server.Models;
using VisionCheckAI.Server.Services;

namespace VisionCheckAI.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InspectionsController : ControllerBase
{
    private readonly VisionCheckDbContext _db;
    private readonly IInferenceService _inferenceService;
    private readonly IWebHostEnvironment _env;

    public InspectionsController(
        VisionCheckDbContext db,
        IInferenceService inferenceService,
        IWebHostEnvironment env)
    {
        _db = db;
        _inferenceService = inferenceService;
        _env = env;
    }

    [HttpPost("upload")]
    public async Task<ActionResult<InspectionDto>> UploadInspection(
        [FromForm] IFormFile file,
        [FromForm] string productId)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded." });
        }

        var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads");
        Directory.CreateDirectory(uploadsFolder);

        var fileId = Guid.NewGuid().ToString("N");
        var fileExt = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(fileExt)) fileExt = ".jpg";
        var fileName = $"{fileId}{fileExt}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativeUrl = $"/uploads/{fileName}";

        // Run AI Inference
        var inference = await _inferenceService.RunInferenceAsync(filePath);

        var isDefective = !string.Equals(inference.Prediction, "Excellent", StringComparison.OrdinalIgnoreCase);
        var resultText = isDefective ? "Defective" : "Pass";

        string? severity = null;
        if (isDefective)
        {
            severity = inference.Prediction switch
            {
                "Fracture" => "Critical",
                "Deformation" => "High",
                "Rusting" => "Medium",
                "Scratches" => "Low",
                _ => inference.Confidence > 0.90 ? "High" : "Medium"
            };
        }

        var product = await _db.Products.FindAsync(productId)
                      ?? await _db.Products.FirstOrDefaultAsync();

        var operatorName = User.Identity?.Name ?? "Operator";

        var entity = new InspectionEntity
        {
            ProductId = product?.Id ?? productId,
            ImageUrl = relativeUrl,
            ThumbnailUrl = relativeUrl,
            Result = resultText,
            Confidence = inference.Confidence,
            Severity = severity,
            OperatorName = operatorName,
            InspectedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            ReviewStatus = "Pending"
        };

        if (isDefective)
        {
            entity.Defects.Add(new DefectEntity
            {
                Category = inference.Prediction,
                Severity = severity ?? "Medium",
                Confidence = inference.Confidence,
                BboxX = 0.2,
                BboxY = 0.2,
                BboxWidth = 0.6,
                BboxHeight = 0.6
            });
        }

        _db.Inspections.Add(entity);
        await _db.SaveChangesAsync();

        return Ok(MapToDto(entity, product));
    }

    [HttpPost("{id}/review")]
    public async Task<ActionResult<InspectionDto>> ReviewInspection(
        string id,
        [FromBody] ReviewRequest request)
    {
        var entity = await _db.Inspections
            .Include(i => i.Defects)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (entity == null)
        {
            return NotFound(new { message = $"Inspection with ID {id} not found." });
        }

        var reviewerName = User.Identity?.Name ?? "Supervisor";

        entity.ReviewedBy = reviewerName;
        entity.ReviewedAtUtc = DateTime.UtcNow;
        entity.ReviewNotes = request.Notes;

        if (request.IsConfirmed)
        {
            entity.ReviewStatus = "Confirmed";
        }
        else
        {
            entity.ReviewStatus = "Overridden";
            if (!string.IsNullOrWhiteSpace(request.CorrectedResult))
            {
                entity.CorrectedResult = request.CorrectedResult;
                entity.Result = request.CorrectedResult;
            }
        }

        await _db.SaveChangesAsync();

        var product = await _db.Products.FindAsync(entity.ProductId);
        return Ok(MapToDto(entity, product));
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<InspectionDto>>> GetInspections([FromQuery] InspectionQuery query)
    {
        var dbQuery = _db.Inspections.Include(i => i.Defects).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.ProductId))
        {
            dbQuery = dbQuery.Where(i => i.ProductId == query.ProductId);
        }

        if (query.FromUtc.HasValue)
        {
            dbQuery = dbQuery.Where(i => i.InspectedAtUtc >= query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            dbQuery = dbQuery.Where(i => i.InspectedAtUtc <= query.ToUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Result))
        {
            dbQuery = dbQuery.Where(i => i.Result == query.Result);
        }

        if (!string.IsNullOrWhiteSpace(query.Severity))
        {
            dbQuery = dbQuery.Where(i => i.Severity == query.Severity);
        }

        if (!string.IsNullOrWhiteSpace(query.DefectCategory))
        {
            dbQuery = dbQuery.Where(i => i.Defects.Any(d => d.Category == query.DefectCategory));
        }

        var totalCount = await dbQuery.CountAsync();

        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 25 : query.PageSize;

        var items = await dbQuery
            .OrderByDescending(i => i.InspectedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var productMap = await _db.Products.ToDictionaryAsync(p => p.Id);

        var dtos = items.Select(i => MapToDto(i, i.ProductId != null && productMap.ContainsKey(i.ProductId) ? productMap[i.ProductId] : null)).ToList();

        return Ok(new PagedResult<InspectionDto>
        {
            Items = dtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    private static InspectionDto MapToDto(InspectionEntity entity, ProductEntity? product)
    {
        return new InspectionDto
        {
            Id = entity.Id,
            ProductId = entity.ProductId,
            ProductName = product?.Name,
            ProductCode = product?.Code,
            ImageUrl = entity.ImageUrl,
            ThumbnailUrl = entity.ThumbnailUrl,
            ImageWidth = entity.ImageWidth,
            ImageHeight = entity.ImageHeight,
            InspectedAtUtc = entity.InspectedAtUtc,
            CreatedAtUtc = entity.CreatedAtUtc,
            OperatorName = entity.OperatorName,
            Result = entity.Result,
            Confidence = entity.Confidence,
            Severity = entity.Severity,
            Defects = entity.Defects.Select(d => new DefectDto
            {
                Category = d.Category,
                Severity = d.Severity,
                Confidence = d.Confidence,
                BoundingBox = new BoundingBoxDto
                {
                    X = d.BboxX,
                    Y = d.BboxY,
                    Width = d.BboxWidth,
                    Height = d.BboxHeight
                }
            }).ToList(),
            Review = string.IsNullOrEmpty(entity.ReviewStatus) || entity.ReviewStatus == "Pending" && string.IsNullOrEmpty(entity.ReviewedBy)
                ? new ReviewDto { Status = entity.ReviewStatus ?? "Pending" }
                : new ReviewDto
                {
                    Status = entity.ReviewStatus,
                    ReviewedBy = entity.ReviewedBy,
                    ReviewedAtUtc = entity.ReviewedAtUtc,
                    CorrectedResult = entity.CorrectedResult,
                    Notes = entity.ReviewNotes
                }
        };
    }
}
