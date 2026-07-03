using System.Globalization;

namespace Mical.Helpers;

/// <summary>
/// Formato de precios en convención argentina ("$1.234,56"). Se usa solo para
/// mostrar: el parseo de formularios sigue siendo con cultura invariante (los
/// &lt;input type="number"&gt; envían el decimal con punto).
/// </summary>
public static class MoneyExtensions
{
    private static readonly NumberFormatInfo Ar = new()
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = ".",
        NumberGroupSizes = new[] { 3 }
    };

    public static string ToMoney(this decimal value) => "$" + value.ToString("#,##0.00", Ar);

    public static string ToMoney(this decimal? value) => (value ?? 0m).ToMoney();
}
