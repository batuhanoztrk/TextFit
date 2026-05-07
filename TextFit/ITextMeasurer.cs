namespace TextFit;

/// <summary>
/// Measures the rendered width of text at a given font size.
/// </summary>
/// <remarks>
/// <para>
/// Implement this to plug your font/graphics library of choice into <see cref="TextFitter"/>.
/// Reference implementations live in companion packages (e.g. <c>TextFit.Skia</c>).
/// </para>
/// <para>
/// <see cref="TextFitter"/> calls <see cref="MeasureWidth"/> many times during a single fit
/// (binary-search across font sizes &#215; word-wrapping iterations), so implementations
/// should be inexpensive — typically by caching font/typeface objects rather than constructing them
/// per call.
/// </para>
/// </remarks>
public interface ITextMeasurer
{
    /// <summary>
    /// Returns the rendered width of <paramref name="text"/> when drawn at <paramref name="fontSize"/>.
    /// </summary>
    /// <param name="text">The text to measure. Never <c>null</c>; may be empty.</param>
    /// <param name="fontSize">The font size, in the same units used by the consumer (typically points or pixels).</param>
    /// <returns>The width of the rendered text in the same units as <paramref name="fontSize"/>.</returns>
    float MeasureWidth(string text, float fontSize);
}