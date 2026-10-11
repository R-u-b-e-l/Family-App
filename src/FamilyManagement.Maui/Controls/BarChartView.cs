using FamilyManagement.Shared.Dtos;

namespace FamilyManagement.Maui.Controls;

/// <summary>Grouped income/expense bar chart drawn with GraphicsView (no chart package needed).</summary>
public class BarChartView : GraphicsView
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource), typeof(IList<MonthlyAmountDto>), typeof(BarChartView),
        propertyChanged: (b, _, _) => ((BarChartView)b).Invalidate());

    public static readonly BindableProperty LabelColorProperty = BindableProperty.Create(
        nameof(LabelColor), typeof(Color), typeof(BarChartView), Colors.Gray,
        propertyChanged: (b, _, _) => ((BarChartView)b).Invalidate());

    public IList<MonthlyAmountDto>? ItemsSource
    {
        get => (IList<MonthlyAmountDto>?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public Color LabelColor
    {
        get => (Color)GetValue(LabelColorProperty);
        set => SetValue(LabelColorProperty, value);
    }

    public BarChartView()
    {
        Drawable = new BarChartDrawable(this);
        HeightRequest = 190;
    }

    private new void Invalidate() => base.Invalidate();

    private class BarChartDrawable(BarChartView owner) : IDrawable
    {
        private static readonly Color IncomeColor = Color.FromArgb("#1D9E5F");
        private static readonly Color ExpenseColor = Color.FromArgb("#D64545");

        public void Draw(ICanvas canvas, RectF rect)
        {
            var items = owner.ItemsSource;
            if (items is null || items.Count == 0)
                return;

            const float bottomSpace = 22f;
            const float topSpace = 8f;
            var plotHeight = rect.Height - bottomSpace - topSpace;
            var max = (float)Math.Max((double)items.Max(i => Math.Max(i.Income, i.Expense)), 1);

            var groupWidth = rect.Width / items.Count;
            var barWidth = Math.Min(groupWidth * 0.28f, 22f);

            canvas.FontSize = 10;
            canvas.FontColor = owner.LabelColor;
            canvas.StrokeColor = owner.LabelColor.WithAlpha(0.25f);
            canvas.StrokeSize = 1;
            canvas.DrawLine(0, rect.Height - bottomSpace, rect.Width, rect.Height - bottomSpace);

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var centerX = groupWidth * i + groupWidth / 2f;

                var incomeHeight = (float)item.Income / max * plotHeight;
                var expenseHeight = (float)item.Expense / max * plotHeight;
                var baseY = rect.Height - bottomSpace;

                canvas.FillColor = IncomeColor;
                canvas.FillRoundedRectangle(centerX - barWidth - 2, baseY - incomeHeight, barWidth, incomeHeight, 3);
                canvas.FillColor = ExpenseColor;
                canvas.FillRoundedRectangle(centerX + 2, baseY - expenseHeight, barWidth, expenseHeight, 3);

                canvas.DrawString(item.Label, centerX - groupWidth / 2f, baseY + 4, groupWidth, 16,
                    HorizontalAlignment.Center, VerticalAlignment.Top);
            }
        }
    }
}
