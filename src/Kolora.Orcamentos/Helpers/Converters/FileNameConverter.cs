using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace Kolora.Orcamentos.Helpers.Converters;

/// <summary>Mostra só o nome do ficheiro (ORC-XXXX.pdf) em vez do caminho completo.</summary>
public class FileNameConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string s && !string.IsNullOrWhiteSpace(s) ? Path.GetFileName(s) : "-";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
