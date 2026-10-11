using FamilyManagement.Shared.Dtos;

namespace FamilyManagement.Maui.Controls;

/// <summary>Category breakdown donut with an in-canvas legend.</summary>
public class DonutChartView : GraphicsView
{
    public static readonly string[] PaletteHex =
    [
        "#1D7A62", "#D64545", "#3D7BE0", "#E09B3D", "#8A5BD6", "#2BABB8", "#C94F8C", "#7A8A3D"
    ];

    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource), typeof(IList<CategoryAmountDto>), typeof(DonutChartView),
        propertyChanged: (b, _, _) => ((DonutChartView)b).Invalidate());

    public static readonly BindableProperty LabelColorProperty = BindableProperty.Create(
        nameof(LabelColor), typeof(Color), typeof(DonutChartView), Colors.Gray,
        propertyChanged: (b, _, _) => ((DonutChartView)b).Invalidate());

    public IList<CategoryAmountDto>? ItemsSource
    {
        get => (IList<CategoryAmountDto>?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public Color LabelColor
    {
        get => (Color)GetValue(LabelColorProperty);
        set => SetValue(LabelColorProperty, value);
    }

    public DonutChartView()
    {
        Drawable = new DonutDrawable(this);
        HeightRequest = 180;
    }

    private new void Invalidate() => base.Invalidate();

    private class DonutDrawable(DonutChartView owner) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF rect)
        {
            var items = owner.ItemsSource?.Where(i => i.Amount > 0).Take(6).ToList();
            if (items is null || items.Count == 0)
                return;

            var total = items.Sum(i => i.Amount);
            if (total <= 0)
                return;

            var size = Math.Min(rect.Height, rect.Width * 0.45f) - 10;
            var cx = rect.Width * 0.24f;
            var cy = rect.Height / 2f;
            var outer = size / 2f;
            var thickness = outer * 0.38f;

            float startAngle = 90f; // MAUI arcs: counter-clockwise from 3 o'clock
            for (var i = 0; i < items.Count; i++)
            {
                var sweep = (float)(items[i].Amount / total) * 360f;
                canvas.StrokeColor = Color.FromArgb(PaletteHex[i % PaletteHex.Length]);
                canvas.StrokeSize = thickness;
                canvas.DrawArc(cx - outer + thickness / 2, cy - outer + thickness / 2,
                    (outer - thickness / 2) * 2, (outer - thickness / 2) * 2,
                    startAngle - sweep, startAngle, true, false);
                startAngle -= sweep;
            }

            // Legend
            canvas.FontSize = 11;
            var legendX = rect.Width * 0.47f;
            var rowHeight = Math.Min(24f, rect.Height / (items.Count + 0.5f));
            var legendY = cy - rowHeight * items.Count / 2f;
            for (var i = 0; i < items.Count; i++)
            {
                var y = legendY + i * rowHeight;
                canvas.FillColor = Color.FromArgb(PaletteHex[i % PaletteHex.Length]);
                canvas.FillRoundedRectangle(legendX, y + rowHeight / 2 - 5, 10, 10, 2);
                canvas.FontColor = owner.LabelColor;
                var pct = (double)(items[i].Amount / total) * 100;
                var name = items[i].Category.Length > 16 ? items[i].Category[..15] + "…" : items[i].Category;
                canvas.DrawString($"{name}  {pct:0}%", legendX + 16, y, rect.Width - legendX - 16, rowHeight,
                    HorizontalAlignment.Left, VerticalAlignment.Center);
            }
        }
    }
}
