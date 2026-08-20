using VisionCheckAI.Client.Models;

namespace VisionCheckAI.Client.Services.Fakes;

/// <summary>
/// In-memory stand-ins for the REST API, used only when Api:UseFakeData is true.
/// They let the whole UI be exercised with no backend running. Nothing here ships
/// to production: flip the flag off and the real HTTP clients are registered instead.
/// </summary>
internal static class FakeData
{
    private static readonly Random Random = new(20260820);

    public static readonly List<ProductDto> Products = new()
    {
        new ProductDto { Id = "101", Code = "M12-HX", Name = "Hex nut M12", Category = "Fastener" },
        new ProductDto { Id = "102", Code = "M16-FL", Name = "Flange nut M16", Category = "Fastener" },
        new ProductDto { Id = "103", Code = "M10-NY", Name = "Nylock nut M10", Category = "Fastener" },
        new ProductDto { Id = "104", Code = "M20-HX", Name = "Hex nut M20", Category = "Fastener" }
    };

    private static readonly string[] Operators = { "S. Patel", "R. Mishra", "A. Das", "K. Sahu", "P. Nayak" };

    private static readonly (string Category, string Image)[] DefectSamples =
    {
        (DefectCategories.Rusting, "img/samples/rusty.jpg"),
        (DefectCategories.Rusting, "img/samples/rusty2.jpg"),
        (DefectCategories.Scratches, "img/samples/scratch1.jpg"),
        (DefectCategories.Scratches, "img/samples/scratch2.jpg"),
        (DefectCategories.Deformation, "img/samples/deform1.jpg"),
        (DefectCategories.Fracture, "img/samples/fracture1.jpg")
    };

    private static readonly string[] PassImages = { "img/samples/excel1.jpg", "img/samples/excel2.jpg" };

    /// <summary>The seeded history, newest first. Mutated by uploads and reviews during a session.</summary>
    public static readonly List<InspectionDto> Inspections = BuildHistory(140);

    private static List<InspectionDto> BuildHistory(int count)
    {
        var list = new List<InspectionDto>(count);
        var clock = DateTime.UtcNow;

        for (var i = 0; i < count; i++)
        {
            // Roughly one in six parts is rejected, spread back over the last two weeks.
            var defective = Random.Next(0, 6) == 0;
            clock = clock.AddMinutes(-Random.Next(80, 220));

            list.Add(Create(
                Products[Random.Next(Products.Count)],
                defective,
                clock,
                Operators[Random.Next(Operators.Length)],
                reviewed: i > 6));
        }

        return list;
    }

    public static InspectionDto Create(
        ProductDto product,
        bool defective,
        DateTime inspectedAtUtc,
        string operatorName,
        bool reviewed)
    {
        var inspection = new InspectionDto
        {
            Id = Guid.NewGuid().ToString(),
            ProductId = product.Id,
            ProductName = product.Name,
            ProductCode = product.Code,
            InspectedAtUtc = inspectedAtUtc,
            OperatorName = operatorName,
            ImageWidth = 640,
            ImageHeight = 640,
            Result = defective ? InspectionResults.Defective : InspectionResults.Pass,
            Review = new ReviewDto { Status = "Pending" }
        };

        if (defective)
        {
            var (category, image) = DefectSamples[Random.Next(DefectSamples.Length)];
            var severity = Severities.All[Random.Next(Severities.All.Length)];

            inspection.ImageUrl = image;
            inspection.ThumbnailUrl = image;
            inspection.Severity = severity;
            inspection.Confidence = Math.Round(0.78 + (Random.NextDouble() * 0.21), 3);

            // Pixel-space box, to exercise the non-normalised overlay path.
            inspection.Defects.Add(new DefectDto
            {
                Category = category,
                Severity = severity,
                Confidence = inspection.Confidence,
                BoundingBox = new BoundingBoxDto
                {
                    X = Random.Next(60, 200),
                    Y = Random.Next(60, 200),
                    Width = Random.Next(180, 300),
                    Height = Random.Next(160, 280)
                }
            });

            // Sometimes a second, weaker detection using normalised coordinates.
            if (Random.Next(0, 3) == 0)
            {
                inspection.Defects.Add(new DefectDto
                {
                    Category = DefectCategories.Scratches,
                    Severity = Severities.Low,
                    Confidence = Math.Round(0.31 + (Random.NextDouble() * 0.2), 3),
                    BoundingBox = new BoundingBoxDto { X = 0.58, Y = 0.24, Width = 0.22, Height = 0.16 }
                });
            }
        }
        else
        {
            var image = PassImages[Random.Next(PassImages.Length)];
            inspection.ImageUrl = image;
            inspection.ThumbnailUrl = image;
            inspection.Confidence = Math.Round(0.9 + (Random.NextDouble() * 0.09), 3);
        }

        if (reviewed && Random.Next(0, 3) != 0)
        {
            var overridden = Random.Next(0, 5) == 0;

            inspection.Review = new ReviewDto
            {
                Status = overridden ? "Overridden" : "Confirmed",
                ReviewedBy = Operators[Random.Next(Operators.Length)],
                ReviewedAtUtc = inspectedAtUtc.AddMinutes(Random.Next(5, 90)),
                CorrectedResult = overridden
                    ? (defective ? InspectionResults.Pass : InspectionResults.Defective)
                    : null,
                Notes = overridden ? "Re-checked under the bench lamp; AI call was wrong." : null
            };
        }

        return inspection;
    }

    /// <summary>Stands in for network latency so loading skeletons are actually visible.</summary>
    public static Task DelayAsync(int minMs = 320, int maxMs = 900) =>
        Task.Delay(Random.Next(minMs, maxMs));
}

public sealed class FakeAuthApi : IAuthApi
{
    private readonly SessionStore _session;

    public FakeAuthApi(SessionStore session) => _session = session;

    /// <summary>
    /// Any password is accepted. The username picks the role, so role gating can be
    /// tested: "inspector" / "supervisor" / "admin". Use "fail" to see the error state.
    /// </summary>
    public async Task SignInAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        await FakeData.DelayAsync();

        if (string.Equals(username, "fail", StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException("Username or password is incorrect.", System.Net.HttpStatusCode.Unauthorized);
        }

        var role = username.ToLowerInvariant() switch
        {
            var u when u.Contains("admin") => UserRoles.Administrator,
            var u when u.Contains("super") => UserRoles.Supervisor,
            _ => UserRoles.Inspector
        };

        await _session.SignInAsync($"fake.{Guid.NewGuid():N}.token", new UserSession
        {
            Id = "1",
            Username = username,
            DisplayName = username.Length <= 2
                ? username.ToUpperInvariant()
                : char.ToUpperInvariant(username[0]) + username[1..],
            Role = role,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(8)
        });
    }

    public Task SignOutAsync() => _session.SignOutAsync();
}

public sealed class FakeProductApi : IProductApi
{
    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        await FakeData.DelayAsync(150, 400);
        return FakeData.Products;
    }
}

public sealed class FakeInspectionApi : IInspectionApi
{
    private readonly SessionStore _session;

    public FakeInspectionApi(SessionStore session) => _session = session;

    public async Task<InspectionDto> UploadAsync(
        string productId,
        byte[] content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        // Long enough to see the inspection skeleton state properly.
        await FakeData.DelayAsync(1400, 2600);

        var product = FakeData.Products.FirstOrDefault(p => p.Id == productId) ?? FakeData.Products[0];

        // The filename drives the verdict so specific outcomes can be reproduced:
        // anything named like a defect class comes back Defective.
        var name = fileName.ToLowerInvariant();
        var defective =
            name.Contains("rust") || name.Contains("scratch") ||
            name.Contains("deform") || name.Contains("fracture");

        var inspection = FakeData.Create(
            product,
            defective,
            DateTime.UtcNow,
            _session.User?.DisplayName ?? "Operator",
            reviewed: false);

        // Show the image the user actually picked, not a sample.
        inspection.ImageUrl = null;
        inspection.ThumbnailUrl = null;

        FakeData.Inspections.Insert(0, inspection);
        return inspection;
    }

    public async Task<InspectionDto> ReviewAsync(
        string inspectionId,
        ReviewRequest review,
        CancellationToken cancellationToken = default)
    {
        await FakeData.DelayAsync();

        var inspection = FakeData.Inspections.FirstOrDefault(i => i.Id == inspectionId)
            ?? throw new ApiException("The requested record was not found.", System.Net.HttpStatusCode.NotFound);

        inspection.Review = new ReviewDto
        {
            Status = review.IsConfirmed ? "Confirmed" : "Overridden",
            ReviewedBy = _session.User?.DisplayName ?? "Operator",
            ReviewedAtUtc = DateTime.UtcNow,
            CorrectedResult = review.CorrectedResult,
            Notes = review.Notes
        };

        return inspection;
    }

    public async Task<PagedResult<InspectionDto>> SearchAsync(
        InspectionQuery query,
        CancellationToken cancellationToken = default)
    {
        await FakeData.DelayAsync();

        IEnumerable<InspectionDto> results = FakeData.Inspections.OrderByDescending(i => i.Timestamp);

        if (!string.IsNullOrWhiteSpace(query.ProductId))
            results = results.Where(i => i.ProductId == query.ProductId);

        if (query.FromUtc is { } from)
            results = results.Where(i => i.Timestamp >= from);

        if (query.ToUtc is { } to)
            results = results.Where(i => i.Timestamp <= to);

        if (!string.IsNullOrWhiteSpace(query.DefectCategory))
            results = results.Where(i => i.Defects.Any(d =>
                string.Equals(d.Category, query.DefectCategory, StringComparison.OrdinalIgnoreCase)));

        if (!string.IsNullOrWhiteSpace(query.Severity))
            results = results.Where(i =>
                string.Equals(i.Severity, query.Severity, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(query.Result))
            results = results.Where(i => string.Equals(
                i.IsDefective ? InspectionResults.Defective : InspectionResults.Pass,
                query.Result,
                StringComparison.OrdinalIgnoreCase));

        var matched = results.ToList();

        return new PagedResult<InspectionDto>
        {
            Items = matched.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = matched.Count
        };
    }
}

public sealed class FakeDashboardApi : IDashboardApi
{
    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        await FakeData.DelayAsync();

        var all = FakeData.Inspections;
        var defective = all.Where(i => i.IsDefective).ToList();

        return new DashboardSummaryDto
        {
            TotalInspections = all.Count,
            PassCount = all.Count - defective.Count,
            DefectiveCount = defective.Count,
            DefectRate = all.Count == 0 ? 0 : defective.Count / (double)all.Count,

            DefectsByCategory = defective
                .SelectMany(i => i.Defects)
                .Where(d => !string.IsNullOrWhiteSpace(d.Category))
                .GroupBy(d => d.Category!)
                .Select(g => new CategoryCountDto { Category = g.Key, Count = g.Count() })
                .OrderByDescending(c => c.Count)
                .ToList(),

            DefectsBySeverity = Severities.All
                .Select(s => new SeverityCountDto
                {
                    Severity = s,
                    Count = defective.Count(i => string.Equals(i.Severity, s, StringComparison.OrdinalIgnoreCase))
                })
                .ToList(),

            DailyTrend = all
                .Where(i => i.Timestamp is not null)
                .GroupBy(i => i.Timestamp!.Value.Date)
                .OrderBy(g => g.Key)
                .Select(g => new DailyTrendPointDto
                {
                    Date = g.Key,
                    Total = g.Count(),
                    Pass = g.Count(i => !i.IsDefective),
                    Defective = g.Count(i => i.IsDefective)
                })
                .ToList()
        };
    }
}
