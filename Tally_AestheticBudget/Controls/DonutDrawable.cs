namespace Tally_AestheticBudget.Models
{
    /// <summary>Data for one ring segment on the budget donut chart.</summary>
    /// <param name="Category">Display name (e.g. "Food", "Unallocated").</param>
    /// <param name="Spent">Actual amount spent this period.</param>
    /// <param name="Limit">Category spending cap; null means unlimited.</param>
    public record DonutSegment(string Category, decimal Spent, decimal? Limit);
}

namespace Tally_AestheticBudget.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Maui.Graphics;
    using Tally_AestheticBudget.Models;

    /// <summary>
    /// IDrawable for a donut/ring budget chart rendered via GraphicsView.
    ///
    /// ── Segment color strategy ──────────────────────────────────────────────
    /// Starting from the accent color's HSL hue, each segment i (out of n) receives
    /// a hue rotated by (360 / n) × i degrees. Saturation is clamped to [0.40, 0.85]
    /// and lightness to [0.35, 0.65] to keep every derived colour legible on both light
    /// and dark card backgrounds. No palette is hardcoded — only the live AccentColor
    /// acts as a seed, so theme switches instantly produce a harmonically related set.
    ///
    /// ── Layout ──────────────────────────────────────────────────────────────
    /// Top 72 % of the view height is used for the ring; the bottom 28 % for a two-
    /// column legend. The ring's outer radius is 85 % of the available half-dimension;
    /// the inner hole is 60 % of the outer radius.
    ///
    /// Call ApplyTheme() from code-behind whenever IThemeService.ThemeChanged fires,
    /// then call GraphicsView.Invalidate().
    /// </summary>
    public class DonutDrawable : IDrawable
    {
        // ── Data ─────────────────────────────────────────────────────────────────

        private IReadOnlyList<DonutSegment> _segments = [];

        /// <summary>
        /// Setting this property fires <see cref="Invalidated"/> so code-behind can
        /// call GraphicsView.Invalidate() without polling.
        /// </summary>
        public IReadOnlyList<DonutSegment> Segments
        {
            get => _segments;
            set { _segments = value; Invalidated?.Invoke(); }
        }

        /// <summary>Fired when <see cref="Segments"/> changes. Subscribe in code-behind.</summary>
        public event Action? Invalidated;

        /// <summary>Currency symbol for the centre label (e.g. "₱").</summary>
        public string CurrencySymbol { get; set; } = "₱";

        // ── Theme colors ──────────────────────────────────────────────────────────
        // Seeded with reasonable defaults; overwritten by ApplyTheme() on every
        // ThemeChanged event so the chart is always in sync with the active palette.

        public Color AccentColor { get; private set; } = Color.FromArgb("#ff6b6b");
        public Color AccentAlpha { get; private set; } = Color.FromArgb("#20ff6b6b");
        public Color CardBg { get; private set; } = Color.FromArgb("#ffffff");
        public Color TextPrimary { get; private set; } = Color.FromArgb("#1a1a2e");
        public Color TextSecondary { get; private set; } = Color.FromArgb("#888888");
        public Color Border { get; private set; } = Color.FromArgb("#e0e0e0");

        /// <summary>
        /// Called from BudgetPage.xaml.cs on every IThemeService.ThemeChanged event.
        /// Does NOT call Invalidate — the caller must do that after setting colours.
        /// </summary>
        public void ApplyTheme(Color accent, Color accentAlpha, Color textPrimary,
                               Color textSecondary, Color cardBg, Color border)
        {
            AccentColor = accent;
            AccentAlpha = accentAlpha;
            CardBg = cardBg;
            TextPrimary = textPrimary;
            TextSecondary = textSecondary;
            Border = border;
        }

        // ── IDrawable ─────────────────────────────────────────────────────────────

        public void Draw(ICanvas canvas, RectF bounds)
        {
            canvas.SaveState();
            try { DrawInternal(canvas, bounds); }
            finally { canvas.RestoreState(); }
        }

        private void DrawInternal(ICanvas canvas, RectF bounds)
        {
            float w = bounds.Width;
            float h = bounds.Height;
            if (w <= 0 || h <= 0) return;

            // Layout: top portion for ring, bottom portion for legend
            float legendH = Math.Min(80f, h * 0.28f);
            float ringAreaH = h - legendH;

            float cx = w / 2f;
            float cy = ringAreaH / 2f;

            // Ring geometry
            float outerR = Math.Min(cx * 0.92f, cy * 0.92f);
            float innerR = outerR * 0.60f;           // hole: 60 % of outer
            float circleR = (outerR + innerR) / 2f;   // stroke midpoint
            float thickness = outerR - innerR;          // stroke width = ring depth

            // ── Background ring (full circle) ────────────────────────────────────
            canvas.StrokeColor = Border.WithAlpha(0.30f);
            canvas.StrokeSize = thickness;
            // DrawEllipse draws the ellipse outline using current stroke settings,
            // giving us the neutral grey ring for the "unused" portion.
            canvas.DrawEllipse(cx - circleR, cy - circleR, circleR * 2, circleR * 2);

            var segs = Segments;
            if (segs == null || segs.Count == 0 || segs.All(s => s.Spent <= 0))
            {
                DrawEmptyState(canvas, cx, cy, innerR);
                return;
            }

            // ── Coloured segments ─────────────────────────────────────────────────
            // Each segment's visual width is proportional to its Spent relative to the
            // total. If Spent > Limit, the arc is capped visually at the Limit's share
            // of the total (the centre text shows the real numbers).
            decimal totalSpent = segs.Sum(s => s.Spent);
            int n = segs.Count;
            // Start at 12 o'clock (-90°) going clockwise.
            // Angle 0 = 3 o'clock; clockwise = true in screen coordinates.
            float startAngle = -90f;

            for (int i = 0; i < n; i++)
            {
                var seg = segs[i];
                if (seg.Spent <= 0) continue;

                // Cap visual width at limit if one is set; centre text shows actuals
                decimal visualSpent = seg.Limit.HasValue
                    ? Math.Min(seg.Spent, seg.Limit.Value)
                    : seg.Spent;

                float fraction = totalSpent > 0 ? (float)(visualSpent / totalSpent) : 0f;
                float sweepAngle = fraction * 360f;
                float endAngle = startAngle + sweepAngle;

                canvas.StrokeColor = DeriveSegmentColor(AccentColor, i, n);
                canvas.StrokeSize = thickness;
                canvas.DrawArc(
                    cx - circleR, cy - circleR,
                    circleR * 2, circleR * 2,
                    startAngle, endAngle,
                    clockwise: true, closed: false);

                startAngle = endAngle;
            }

            // ── Centre text ───────────────────────────────────────────────────────
            DrawCentreText(canvas, cx, cy, innerR, segs);

            // ── Legend ────────────────────────────────────────────────────────────
            DrawLegend(canvas, w, ringAreaH + 6f, legendH, segs);
        }

        private void DrawEmptyState(ICanvas canvas, float cx, float cy, float innerR)
        {
            float tw = innerR * 1.8f;
            canvas.FontColor = TextSecondary;
            canvas.FontSize = 12f;
            canvas.DrawString("No data", cx - tw / 2f, cy - 9f, tw, 18f,
                HorizontalAlignment.Center, VerticalAlignment.Center);
        }

        private void DrawCentreText(ICanvas canvas, float cx, float cy, float innerR,
                                    IReadOnlyList<DonutSegment> segs)
        {
            decimal totalSpent = segs.Sum(s => s.Spent);
            bool allHaveLimits = segs.All(s => s.Limit.HasValue);
            decimal? totalLimit = allHaveLimits
                ? segs.Sum(s => s.Limit!.Value)
                : (decimal?)null;

            string mainText = $"{CurrencySymbol}{totalSpent:N0}";
            string subText = totalLimit.HasValue
                ? $"of {CurrencySymbol}{totalLimit.Value:N0}"
                : "spent";

            float tw = innerR * 1.7f;
            float tx = cx - tw / 2f;
            float mainSize = Math.Max(11f, Math.Min(20f, innerR * 0.36f));
            float subSize = Math.Max(9f, Math.Min(13f, innerR * 0.22f));
            float lineGap = mainSize * 0.7f;

            canvas.FontColor = AccentColor;
            canvas.FontSize = mainSize;
            canvas.DrawString(mainText, tx, cy - lineGap / 2f - mainSize * 0.3f,
                tw, mainSize + 4f, HorizontalAlignment.Center, VerticalAlignment.Center);

            canvas.FontColor = TextSecondary;
            canvas.FontSize = subSize;
            canvas.DrawString(subText, tx, cy + lineGap / 2f + 2f,
                tw, subSize + 4f, HorizontalAlignment.Center, VerticalAlignment.Center);
        }

        private void DrawLegend(ICanvas canvas, float w, float legendTop,
                                 float legendH, IReadOnlyList<DonutSegment> segs)
        {
            int count = Math.Min(segs.Count, 8); // cap at 8 legend entries
            if (count == 0) return;

            int cols = 2;
            int rows = (int)Math.Ceiling(count / (double)cols);
            float rowH = legendH / Math.Max(rows, 1);
            float colW = w / cols;
            float dotSz = 7f;

            for (int i = 0; i < count; i++)
            {
                int col = i % cols;
                int row = i / cols;

                float lx = colW * col + 12f;
                float ly = legendTop + row * rowH + (rowH - dotSz) / 2f;

                // Coloured dot
                canvas.FillColor = DeriveSegmentColor(AccentColor, i, segs.Count);
                canvas.FillRoundedRectangle(lx, ly, dotSz, dotSz, 2f);

                // Category label
                canvas.FontColor = TextSecondary;
                canvas.FontSize = 8.5f;
                canvas.DrawString(segs[i].Category,
                    lx + dotSz + 5f, ly - 1f,
                    colW - dotSz - 22f, rowH,
                    HorizontalAlignment.Left, VerticalAlignment.Center);
            }
        }

        // ── HSL colour derivation ─────────────────────────────────────────────────

        /// <summary>
        /// Derives a per-segment colour by rotating the accent hue by
        /// <c>(360 / total) × index</c> degrees in HSL space.
        /// S clamped to [0.40, 0.85]; L clamped to [0.35, 0.65].
        /// </summary>
        private static Color DeriveSegmentColor(Color accent, int index, int total)
        {
            RgbToHsl(accent.Red, accent.Green, accent.Blue,
                     out float h, out float s, out float l);

            float newH = (h + 360f / Math.Max(total, 1) * index) % 360f;
            float newS = Math.Max(0.40f, Math.Min(0.85f, s * 0.90f));
            float newL = Math.Max(0.35f, Math.Min(0.65f, l));
            return Color.FromHsla(newH / 360f, newS, newL, 1f);
        }

        private static void RgbToHsl(float r, float g, float b,
                                      out float h, out float s, out float l)
        {
            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            l = (max + min) / 2f;
            h = 0f; s = 0f;
            if (Math.Abs(max - min) < 1e-6f) return;

            float d = max - min;
            s = l > 0.5f ? d / (2f - max - min) : d / (max + min);

            if (Math.Abs(max - r) < 1e-6f) h = (g - b) / d + (g < b ? 6f : 0f);
            else if (Math.Abs(max - g) < 1e-6f) h = (b - r) / d + 2f;
            else h = (r - g) / d + 4f;
            h *= 60f; // convert to degrees [0, 360)
        }
    }
}