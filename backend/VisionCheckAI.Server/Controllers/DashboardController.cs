using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisionCheckAI.Server.Data;
using VisionCheckAI.Server.Models;

namespace VisionCheckAI.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly VisionCheckDbContext _db;

    public DashboardController(VisionCheckDbContext db)
    {
        _db = db;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
    {
        var total = await _db.Inspections.CountAsync();
        var defective = await _db.Inspections.CountAsync(i => i.Result == "Defective");
        var pass = total - defective;
        var defectRate = total == 0 ? 0 : (double)defective / total * 100.0;

        var defects = await _db.Defects.ToListAsync();

        var byCategory = defects
            .GroupBy(d => d.Category)
            .Select(g => new CategoryCountDto
            {
                Category = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(c => c.Count)
            .ToList();

        var bySeverity = defects
            .GroupBy(d => d.Severity)
            .Select(g => new SeverityCountDto
            {
                Severity = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(s => s.Count)
            .ToList();

        // 7-day daily trend
        var startDate = DateTime.UtcNow.Date.AddDays(-6);
        var inspectionsRecent = await _db.Inspections
            .Where(i => i.InspectedAtUtc >= startDate)
            .ToListAsync();

        var dailyTrend = Enumerable.Range(0, 7).Select(i =>
        {
            var date = startDate.AddDays(i);
            var dayInspections = inspectionsRecent.Where(x => x.InspectedAtUtc.Date == date).ToList();
            var dayTotal = dayInspections.Count;
            var dayDefective = dayInspections.Count(x => x.Result == "Defective");
            return new DailyTrendPointDto
            {
                Date = date,
                Total = dayTotal,
                Pass = dayTotal - dayDefective,
                Defective = dayDefective
            };
        }).ToList();

        return Ok(new DashboardSummaryDto
        {
            TotalInspections = total,
            PassCount = pass,
            DefectiveCount = defective,
            DefectRate = defectRate,
            DefectsByCategory = byCategory,
            DefectsBySeverity = bySeverity,
            DailyTrend = dailyTrend
        });
    }
}
