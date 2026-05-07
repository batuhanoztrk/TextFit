namespace TextFit.Skia;

using SkiaSharp;

/// <summary>
/// An <see cref="ITextMeasurer"/> implementation backed by SkiaSharp.
/// </summary>
/// <remarks>
/// <para>
/// Caches the underlying <see cref="SKTypeface"/> and a reusable <see cref="SKPaint"/> for the
/// lifetime of the instance, so a single fitter run only constructs them once instead of per
/// measurement.
/// </para>
/// <para>
/// <b>Ownership.</b> When you construct from a file path, this measurer owns the typeface and
/// will dispose of it. When you pass a pre-built <see cref="SKTypeface"/>, ownership is opt in via
/// the <c>ownsTypeface</c> constructor parameter.
/// </para>
/// <para>
/// <b>Thread safety.</b> Instances are NOT thread-safe — they mutate a shared
/// <see cref="SKPaint"/> on every measurement. Use one per thread or guard externally.
/// </para>
/// </remarks>
public sealed class SkiaTextMeasurer : ITextMeasurer, IDisposable
{
    private readonly SKTypeface _typeface;
    private readonly bool _ownsTypeface;
    private readonly SKPaint _paint;
    private bool _disposed;

    /// <summary>Loads a typeface from a TTF/OTF file path.</summary>
    /// <param name="fontPath">Filesystem path to a font readable by SkiaSharp.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="fontPath"/> is <c>null</c>.</exception>
    /// <exception cref="FileNotFoundException">When <paramref name="fontPath"/> does not exist.</exception>
    /// <exception cref="InvalidOperationException">When SkiaSharp cannot parse the font.</exception>
    public SkiaTextMeasurer(string fontPath)
    {
        if (fontPath == null) throw new ArgumentNullException(nameof(fontPath));
        if (!File.Exists(fontPath)) throw new FileNotFoundException("Font file not found.", fontPath);

        _typeface = SKTypeface.FromFile(fontPath)
                    ?? throw new InvalidOperationException($"SkiaSharp could not load typeface from '{fontPath}'.");
        _ownsTypeface = true;
        _paint = CreatePaint(_typeface);
    }

    /// <summary>Wraps an existing <see cref="SKTypeface"/>.</summary>
    /// <param name="typeface">The typeface to use for measurement.</param>
    /// <param name="ownsTypeface">
    /// When <c>true</c>, the measurer will dispose the typeface in <see cref="Dispose"/>.
    /// Pass <c>false</c> if the typeface is shared with other code that manages its lifetime.
    /// </param>
    /// <exception cref="ArgumentNullException">When <paramref name="typeface"/> is <c>null</c>.</exception>
    public SkiaTextMeasurer(SKTypeface typeface, bool ownsTypeface = false)
    {
        _typeface = typeface ?? throw new ArgumentNullException(nameof(typeface));
        _ownsTypeface = ownsTypeface;
        _paint = CreatePaint(_typeface);
    }

    private static SKPaint CreatePaint(SKTypeface typeface) => new SKPaint
    {
        Typeface = typeface,
        IsAntialias = true,
    };

    /// <inheritdoc />
    public float MeasureWidth(string text, float fontSize)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(SkiaTextMeasurer));
        if (text == null) throw new ArgumentNullException(nameof(text));
        if (text.Length == 0) return 0f;

        _paint.TextSize = fontSize;
        return _paint.MeasureText(text);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _paint.Dispose();
        if (_ownsTypeface) _typeface.Dispose();
        _disposed = true;
    }
}