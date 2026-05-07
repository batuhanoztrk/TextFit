namespace TextFit;

/// <summary>
/// Tunables for <see cref="TextFitter.Fit(string, float, float, FitOptions)"/>.
/// </summary>
public sealed class FitOptions
{
    /// <summary>The smallest font size the fitter will return. Default: <c>5</c>.</summary>
    public float MinFontSize { get; set; } = 5f;

    /// <summary>The largest font size the fitter will return. Default: <c>60</c>.</summary>
    public float MaxFontSize { get; set; } = 60f;

    /// <summary>
    /// Optional cap derived from the box height. When set, the effective max font size is
    /// <c>min(MaxFontSize, max(12, boxHeight * value))</c>. Set to <c>null</c> to disable.
    /// Default: <c>0.55</c>.
    /// </summary>
    /// <remarks>
    /// This prevents absurd font sizes in tall, narrow boxes (e.g., a 4&#215;200 box would
    /// otherwise pick a 60pt font that can't render anything legible).
    /// </remarks>
    public float? MaxFontSizeBoxHeightFactor { get; set; } = 0.55f;

    /// <summary>
    /// Inner margin in the same units as the box dimensions. When negative (default), the
    /// margin is auto-computed as <c>max(2, min(boxWidth, boxHeight) * 0.08)</c>.
    /// </summary>
    public float Margin { get; set; } = -1f;

    /// <summary>
    /// Multiplier between font size and line height (leading). <c>1.0</c> means lines are
    /// stacked with no extra spacing; <c>1.2</c> leaves 20% extra space. Default: <c>1.12</c>.
    /// </summary>
    public float LineSpacingFactor { get; set; } = 1.12f;

    /// <summary>
    /// Number of binary-search iterations used to home in on the largest fitting font size.
    /// 18 yields precision of <c>(MaxFontSize - MinFontSize) / 2^18</c> — well below a single
    /// pixel for typical inputs. Default: <c>18</c>.
    /// </summary>
    public int BinarySearchIterations { get; set; } = 18;
}