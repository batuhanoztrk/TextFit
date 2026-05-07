# TextFit

A small, renderer-agnostic .NET library for fitting wrapped text into a fixed-size bounding box at the **largest font size that still fits**.

It does three things:

1. **Wraps** your text by word.
2. **Picks** the largest font size whose wrapped output fits — via binary search.
3. **Returns** the chosen size and per-line positions, ready to hand to your renderer.

It is **not** tied to any specific drawing library. You provide a tiny `ITextMeasurer` implementation that knows how to measure text in your font; TextFit does the layout. A SkiaSharp-backed measurer ships in the optional `TextFit.Skia` package.

---

## Why

Drawing wrapped, auto-fitting text into a fixed box (PDF signature widgets, image overlays, badges, fixed-cell labels…) is annoying: you have to wrap, measure, search for a font size that fits, and then translate line indices into your renderer's coordinate system. TextFit factors out the measure-search-wrap loop, so the only thing you write is the actual drawing call.

---

## Install

```bash
dotnet add package TextFit          # core
dotnet add package TextFit.Skia     # optional, ITextMeasurer using SkiaSharp
```

> Both packages target `netstandard2.0` and `net8.0`.

---

## Quick start

```csharp
using TextFit;
using TextFit.Skia;

// 1. Build a measurer once. Reuse it across many fits — typeface load is cached.
using var measurer = new SkiaTextMeasurer("/path/to/Font-Regular.ttf");

// 2. Build the fitter.
var fitter = new TextFitter(measurer);

// 3. Fit text into a 200 x 100 box.
FitResult result = fitter.Fit(
    text:      "Hello, this is some text that may wrap and shrink to fit.",
    boxWidth:  200,
    boxHeight: 100);

// 4. Render. EnumerateTopLeft gives you (text, x, y-of-line-top) tuples in
//    a top-left-origin coordinate space.
foreach (var (line, x, y) in result.EnumerateTopLeft())
{
    myCanvas.DrawText(line, x, y, fontSize: result.FontSize);
}
```

## Drawing into a PDF (bottom-left origin, baseline-positioned)

PDF coordinates start at the bottom-left of the page (or widget) with Y increasing upward, and text APIs typically positioned by baseline. Use `EnumerateBaselineBottomLeft`:

```csharp
var result = fitter.Fit(text, boxWidth, boxHeight);

int fontSize = (int)Math.Truncate(result.FontSize);

foreach (var (line, x, y) in result.EnumerateBaselineBottomLeft(boxHeight))
{
    pdfWidget.AddText(
        line,
        (int)Math.Round(x),
        (int)Math.Round(y),
        fontSize.ToString(CultureInfo.InvariantCulture));
}
```

## Custom measurer (no SkiaSharp dependency)

Implement `ITextMeasurer` in whatever way suits your stack:

```csharp
public sealed class MyMeasurer : ITextMeasurer
{
    public float MeasureWidth(string text, float fontSize)
    {
        // …however your toolkit measures text. The result must be in the same
        // units you'll later pass as the box width to TextFitter.Fit().
    }
}
```

Common implementations:

- **SkiaSharp** — `TextFit.Skia.SkiaTextMeasurer` (provided).
- **System.Drawing** — wrap `Graphics.MeasureString`.
- **PdfSharp / iText / QuestPDF** — use the library's font metrics API.
- **Anything with a font file** — parse glyph advance widths from the OpenType `hmtx` table.

## Options

```csharp
var options = new FitOptions
{
    MinFontSize                = 6,      // smallest font the search will return
    MaxFontSize                = 48,     // largest font the search will return
    Margin                     = 4,      // -1 = auto (default: 8% of min side, floor 2)
    LineSpacingFactor          = 1.2f,   // line height = font size * this
    BinarySearchIterations     = 20,     // precision of the size search
    MaxFontSizeBoxHeightFactor = 0.5f,   // additional cap: max(12, height * value); null disables
};

var result = fitter.Fit(text, w, h, options);
```

## How it works

```
                ┌─────── boxWidth ────────┐
                ┌──────────────────────────┐ ─┐
                │ ░░ margin ░░░░░░░░░░░░░░ │  │
                │ ░░ Hello, this is some   │  │
                │ ░░ text that may wrap    │  │ boxHeight
                │ ░░ and shrink to fit.    │  │
                │ ░░░░░░░░░░░░░░░░░░░░░░░░ │  │
                └──────────────────────────┘ ─┘
```

`TextFitter.Fit` binary-searches the font size in `[MinFontSize, MaxFontSize]`. For each candidate size it word-wraps the text using your measurer; the size is **accepted** when:

- every wrapped line fits within `boxWidth - 2*margin`, and
- the total stack height (`lines.Count * fontSize * LineSpacingFactor`) fits within `boxHeight - 2*margin`.

The search probes higher after an accept, lower after a reject, and returns the largest accepted size after `BinarySearchIterations` rounds.

**Long single words.** A token wider than the available width can't be broken — when this happens at the current candidate size, the candidate is rejected and the search picks a smaller font. This guarantees no mid-word truncation.

**Newlines.** `\n` and `\r\n` are honored as paragraph breaks. Empty paragraphs render as a blank line (preserving the gap between paragraphs).

## Coordinate system cheat sheet

| Method                              | Origin       | Y direction   | Position of `(X, Y)` per line                    |
| ----------------------------------- | ------------ | ------------- | ------------------------------------------------ |
| `EnumerateTopLeft()`                | Top-left     | Y goes down   | Top of the line's box (add `FontSize` for baseline) |
| `EnumerateBaselineBottomLeft(h)`    | Bottom-left  | Y goes up     | Baseline of the line                             |

## Building

```bash
git clone https://github.com/batuhanoztrk/TextFit
cd TextFit
dotnet build
dotnet pack -c Release          # produces .nupkg files in artifacts/
```

## Versioning

This project follows [SemVer](https://semver.org/). It is currently `0.x` — the public API may still change between minor versions.

## License

[MIT](LICENSE).
