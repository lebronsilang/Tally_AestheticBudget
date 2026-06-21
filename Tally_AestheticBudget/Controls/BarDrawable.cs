namespace Tally_AestheticBudget.Models
{
    /// <summary>Data for one vertical bar in the spending bar chart.</summary>
    /// <param name="Label">X-axis label (e.g. "Mon", "Wk 2", "Jan").</param>
    /// <param name="Value">Total spending for this period bucket.</param>
    public record BarDataPoint(string Label, decimal Value);
}

namespace Tally_AestheticBudget.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Maui.Graphics;
    using Tally_AestheticBudget.Models;

    /// <summary>
    /// IDrawable for a vertical bar chart rendered via GraphicsView.
    ///
    /// ── Layout ──────────────────────────────────────────────────────────────
    /// Left pad = 36 dp — space for compact y-axis labels.
    /// Bottom pad = 26 dp — space for x-axis labels.
    /// Four dotted horizontal reference lines at 25 / 50 / 75 / 100 % of MaxValue.
    /// Background bars (AccentAlpha, full height) show the ceiling; filled bars
    /// (AccentColor, proportional height) rise to the actual value. Both bars use
    /// rounded top corners only — bottoms are square so they sit flush on the floor.
    ///
    /// Call ApplyTheme() then GraphicsView.Invalidate() whenever ThemeChanged fires.
    /// </summary>
    public class BarDrawable : IDrawable
    {
        // ── Data ─────────────────────────────────────────────────────────────────

        private IReadOnlyList<BarDataPoint> _points = [];

        /// <summary>
        /// Replaces the current data and fires <see cref="Invalidated"/> so code-behind
        /// can call GraphicsView.Invalidate() without polling.
        /// Always set <see cref="MaxValue"/> before setting this property so the first
        /// render has the correct ceiling.
        /// </summary>
        public IReadOnlyList<BarDataPoint> Points
        {
            get => _points;
            set { _points = value; Invalidated?.Invoke(); }
        }

        /// <summary>Fired when <see cref="Points"/> is replaced.</summary>
        public event Action? Invalidated;

        /// <summary>
        /// Y-axis ceiling for the chart.  Set this to Max(Value) — or any meaningful
        /// ceiling — before assigning <see cref="Points"/> to avoid a stale first render.
        /// </summary>
        public decimal MaxValue { get; set; }

        // ── Theme colors ──────────────────────────────────────────────────────────

        private Color _accentColor = Color.FromArgb("#ff6b6b");
        private Color _accentAlpha = Color.FromArgb("#33ff6b6b");
        private Color _textPrimary = Color.FromArgb("#1c1c1e");
        private Color _textSecondary = Color.FromArgb("#8e8e93");
        private Color _cardBg = Color.FromArgb("#ffffff");
        private Color _border = Color.FromArgb("#e5e5ea");

        /// <summary>
        /// Syncs all six color tokens from the live resource dictionary.
        /// Call this immediately after ThemeChanged fires, then Invalidate() the view.
        /// </summary>
        public void ApplyTheme(Color accent, Color accentAlpha,
            Color textPrimary, Color textSecondary, Color cardBg, Color border)
        {
            _accentColor = accent;
            _accentAlpha = accentAlpha;
            _textPrimary = textPrimary;
            _textSecondary = textSecondary;
            _cardBg = cardBg;
            _border = border;
        }

        // ── IDrawable ─────────────────────────────────────────────────────────────

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.SaveState();

            float w = dirtyRect.Width;
            float h = dirtyRect.Height;

            const float leftPad = 38f;
            const float rightPad = 8f;
            const float topPad = 8f;
            const float bottomPad = 26f;

            float chartLeft = leftPad;
            float chartRight = w - rightPad;
            float chartTop = topPad;
            float chartBottom = h - bottomPad;
            float chartW = chartRight - chartLeft;
            float chartH = chartBottom - chartTop;

            // ── Empty / zero state ────────────────────────────────────────────────
            if (_points.Count == 0 || MaxValue <= 0)
            {
                DrawEmptyState(canvas, dirtyRect);
                canvas.RestoreState();
                return;
            }

            // ── Dotted Y-axis reference lines ────────────────────────────────────
            canvas.SaveState();
            canvas.StrokeColor = _textSecondary.WithAlpha(0.30f);
            canvas.StrokeSize = 0.8f;
            canvas.StrokeDashPattern = [3f, 3f];

            for (int r = 1; r <= 4; r++)
            {
                float pct = r / 4f;
                float yRef = chartBottom - chartH * pct;

                // Reference line
                canvas.DrawLine(chartLeft, yRef, chartRight, yRef);

                // Y-axis label (compact: 1.5k, 500, etc.)
                decimal labelVal = MaxValue * (decimal)pct;
                string labelStr = labelVal >= 10_000m
                    ? $"{(int)(labelVal / 1000m)}k"
                    : labelVal >= 1_000m
                        ? $"{labelVal / 1000m:F1}k"
                        : $"{(int)labelVal}";

                canvas.FontColor = _textSecondary;
                canvas.FontSize = 9f;
                canvas.DrawString(
                    labelStr,
                    0f, yRef - 6f, leftPad - 3f, 14f,
                    HorizontalAlignment.Right, VerticalAlignment.Top);
            }
            canvas.RestoreState();

            // ── Bars ──────────────────────────────────────────────────────────────
            int count = _points.Count;
            float gapFrac = count <= 4 ? 0.35f : 0.25f;    // tighter gaps when many bars
            float totalGap = chartW * gapFrac;
            float barW = (chartW - totalGap) / count;
            float gap = count > 1 ? totalGap / (count + 1) : 0f;
            float cornerR = Math.Min(barW * 0.3f, 5f);

            for (int i = 0; i < count; i++)
            {
                var pt = _points[i];
                float bx = chartLeft + gap * (i + 1) + barW * i;

                // Background bar — full chart height (shows potential ceiling)
                canvas.FillColor = _accentAlpha;
                DrawRoundedTopBar(canvas, bx, chartTop, barW, chartH, cornerR);

                // Filled bar — proportional to value
                if (pt.Value > 0)
                {
                    float fillPct = Math.Clamp((float)(pt.Value / MaxValue), 0f, 1f);
                    float fillH = chartH * fillPct;
                    float fillY = chartBottom - fillH;
                    canvas.FillColor = _accentColor;
                    DrawRoundedTopBar(canvas, bx, fillY, barW, fillH, cornerR);
                }

                // X-axis label
                canvas.FontColor = _textSecondary;
                canvas.FontSize = 9f;
                canvas.DrawString(
                    pt.Label,
                    bx, chartBottom + 3f, barW, bottomPad - 3f,
                    HorizontalAlignment.Center, VerticalAlignment.Top);
            }

            canvas.RestoreState();
        }

        /// <summary>
        /// Fills a rectangle with rounded top-left and top-right corners; bottom corners
        /// remain square so the bar sits flush on the chart floor.
        /// </summary>
        private static void DrawRoundedTopBar(
            ICanvas canvas, float x, float y, float w, float h, float r)
        {
            if (h <= 0 || w <= 0) return;
            r = Math.Min(r, Math.Min(w / 2f, h / 2f));

            var path = new PathF();
            path.MoveTo(x, y + r);                      // start below top-left arc
            path.QuadTo(x, y, x + r, y);          // top-left corner
            path.LineTo(x + w - r, y);
            path.QuadTo(x + w, y, x + w, y + r);            // top-right corner
            path.LineTo(x + w, y + h);                      // bottom-right (square)
            path.LineTo(x, y + h);                      // bottom-left  (square)
            path.Close();

            canvas.FillPath(path);
        }

        private void DrawEmptyState(ICanvas canvas, RectF dirtyRect)
        {
            canvas.FontColor = _textSecondary;
            canvas.FontSize = 12f;
            canvas.DrawString(
                "No data for this period",
                0, 0, dirtyRect.Width, dirtyRect.Height,
                HorizontalAlignment.Center, VerticalAlignment.Center);
        }
    }
}