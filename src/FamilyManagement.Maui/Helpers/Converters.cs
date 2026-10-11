using System.Globalization;
using FamilyManagement.Shared.Enums;

namespace FamilyManagement.Maui.Helpers;

/// <summary>Formats decimals as money with the family's currency symbol.</summary>
public class MoneyConverter : IValueConverter
{
    public static string Symbol { get; set; } = "৳";

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is decimal amount ? $"{Symbol} {amount:N2}" : $"{Symbol} 0.00";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class VoucherTypeNameConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        VoucherType.JV => "Journal",
        VoucherType.PV => "Payment",
        VoucherType.RV => "Receipt",
        VoucherType.CV => "Contra",
        _ => ""
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class VoucherStatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        VoucherStatus.Draft => Color.FromArgb("#8A98A3"),
        VoucherStatus.Posted => Color.FromArgb("#1D9E5F"),
        VoucherStatus.Cancelled => Color.FromArgb("#D64545"),
        VoucherStatus.Reversed => Color.FromArgb("#E09B3D"),
        _ => Colors.Gray
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class VoucherTypeColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        VoucherType.RV => Color.FromArgb("#1D9E5F"),
        VoucherType.PV => Color.FromArgb("#D64545"),
        VoucherType.CV => Color.FromArgb("#3D7BE0"),
        VoucherType.JV => Color.FromArgb("#8A5BD6"),
        _ => Colors.Gray
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class DateOnlyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is DateOnly date ? date.ToString("dd MMM yyyy") : "";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b && !b;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b && !b;
}

public class ZeroToEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is decimal d && d != 0 ? d.ToString("N2") : "";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        decimal.TryParse(value as string, NumberStyles.Any, culture, out var d) ? d : 0m;
}
