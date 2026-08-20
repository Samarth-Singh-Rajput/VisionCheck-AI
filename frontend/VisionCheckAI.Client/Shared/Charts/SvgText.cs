using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace VisionCheckAI.Client.Shared.Charts;

/// <summary>
/// Emits an SVG &lt;text&gt; element. Built in code because Razor reserves the
/// literal &lt;text&gt; tag as a control structure.
/// </summary>
public sealed class SvgText : ComponentBase
{
    [Parameter] public double X { get; set; }
    [Parameter] public double Y { get; set; }

    /// <summary>SVG text-anchor: start | middle | end.</summary>
    [Parameter] public string Anchor { get; set; } = "start";

    [Parameter] public string? Class { get; set; }
    [Parameter] public string? Value { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "text");
        builder.AddAttribute(1, "x", Format(X));
        builder.AddAttribute(2, "y", Format(Y));
        builder.AddAttribute(3, "text-anchor", Anchor);

        if (!string.IsNullOrWhiteSpace(Class))
        {
            builder.AddAttribute(4, "class", Class);
        }

        builder.AddContent(5, Value);
        builder.CloseElement();
    }

    private static string Format(double value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);
}
