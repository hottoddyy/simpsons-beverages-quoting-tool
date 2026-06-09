using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SimpsonsBeverages.QuotingTool.App;

public static class SimplePdfExporter
{
    private static readonly Encoding PdfEncoding = Encoding.ASCII;

    public static void Export(string path, QuotePdfModel quote)
    {
        var logo = LoadLogo();
        var content = BuildPageContent(quote, logo is not null);

        var contentObjectNumber = logo is null ? 6 : 7;
        var pageResources = logo is null
            ? "<< /Font << /F1 4 0 R /F2 5 0 R >> >>"
            : "<< /Font << /F1 4 0 R /F2 5 0 R >> /XObject << /Logo 6 0 R >> >>";

        var objects = new List<byte[]>
        {
            Bytes("<< /Type /Catalog /Pages 2 0 R >>"),
            Bytes("<< /Type /Pages /Kids [3 0 R] /Count 1 >>"),
            Bytes($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources {pageResources} /Contents {contentObjectNumber} 0 R >>"),
            Bytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"),
            Bytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>")
        };

        if (logo is not null)
        {
            objects.Add(BuildImageObject(logo));
        }

        objects.Add(BuildStreamObject(Bytes(content)));
        WritePdf(path, objects);
    }

    private static string BuildPageContent(QuotePdfModel quote, bool hasLogo)
    {
        var builder = new StringBuilder();

        builder.AppendLine("1 1 1 rg");
        builder.AppendLine("0 0 595 842 re f");

        if (hasLogo)
        {
            DrawImage(builder, "Logo", 246, 768, 104, 70);
        }
        else
        {
            builder.AppendLine("0.019 0.165 0.380 rg");
            WriteText(builder, "F2", 20, 236, 796, "SIMPSONS");
        }

        builder.AppendLine("0.019 0.165 0.380 rg");
        WriteText(builder, "F2", 8, 56, 806, DateTime.Today.ToString("dd/MM/yyyy"));

        builder.AppendLine("0.019 0.165 0.380 RG");
        builder.AppendLine("395 784 m 395 824 l S");
        WriteText(builder, "F2", 7, 408, 815, "Simpsons Beverage Supply Company Ltd");
        WriteText(builder, "F1", 7, 408, 803, "Chiswick Grove, Blackpool, FY3 9TW");
        WriteText(builder, "F1", 6, 408, 791, "Company Registration No. 00459409");
        WriteText(builder, "F1", 6, 408, 781, "VAT no: 154531381");

        builder.AppendLine("0.019 0.165 0.380 RG");
        builder.AppendLine("56 744 m 539 744 l S");

        WriteText(builder, "F2", 16, 56, 711, "Customer Quote");
        builder.AppendLine("0.549 0.745 0.812 rg");
        builder.AppendLine("56 698 92 2 re f");

        builder.AppendLine("0.12 0.16 0.20 rg");
        WriteText(builder, "F2", 8, 56, 672, "Customer");
        WriteText(builder, "F1", 8, 128, 672, EmptyFallback(quote.Customer));
        WriteText(builder, "F2", 8, 56, 656, "Date");
        WriteText(builder, "F1", 8, 128, 656, DateTime.Today.ToString("dd/MM/yyyy"));

        var y = 618;
        DrawTableHeader(builder, y, quote.UsagePriceHeader, quote.HasServeCostColumn, quote.ServeCostHeader);
        y -= 25;

        foreach (var line in quote.Lines.Take(18))
        {
            DrawQuoteLine(builder, line, y, quote.HasServeCostColumn);
            y -= 22;
        }

        if (quote.Lines.Count > 18)
        {
            WriteText(builder, "F1", 9, 56, y, $"Plus {quote.Lines.Count - 18} further line(s).");
            y -= 24;
        }

        builder.AppendLine("0.019 0.165 0.380 RG");
        builder.AppendLine("56 118 m 539 118 l S");
        WriteText(builder, "F1", 9, 56, 94, "This quote is valid for 1 week unless otherwise confirmed.");
        WriteText(builder, "F1", 8, 56, 58, "+44 (0)1253 766333");
        WriteText(builder, "F1", 8, 206, 58, "info@simpsonsbeverages.com");
        WriteText(builder, "F1", 8, 390, 58, "www.simpsonsbeverages.com");

        return builder.ToString();
    }

    private static void DrawTableHeader(StringBuilder builder, int y, string usagePriceHeader, bool hasServeCostColumn, string serveCostHeader)
    {
        builder.AppendLine("0.019 0.165 0.380 rg");
        builder.AppendLine($"56 {y - 9} 483 22 re f");
        builder.AppendLine("1 1 1 rg");

        if (hasServeCostColumn)
        {
            WriteText(builder, "F2", 6, 64, y, "CODE");
            WriteText(builder, "F2", 6, 122, y, "DESCRIPTION");
            WriteText(builder, "F2", 6, 318, y, "UNIT");
            WriteText(builder, "F2", 6, 366, y, "\u00A3/UNIT");
            WriteText(builder, "F2", 6, 423, y, usagePriceHeader);
            WriteText(builder, "F2", 6, 493, y, serveCostHeader);
            return;
        }

        WriteText(builder, "F2", 7, 68, y, "CODE");
        WriteText(builder, "F2", 7, 178, y, "DESCRIPTION");
        WriteText(builder, "F2", 7, 346, y, "UNIT");
        WriteText(builder, "F2", 7, 410, y, "\u00A3/UNIT");
        WriteText(builder, "F2", 7, 474, y, usagePriceHeader);
    }

    private static void DrawQuoteLine(StringBuilder builder, QuotePdfLine line, int y, bool hasServeCostColumn)
    {
        builder.AppendLine("0.97 0.985 0.99 rg");
        builder.AppendLine($"56 {y - 8} 483 22 re f");
        builder.AppendLine("0.72 0.80 0.84 RG");
        builder.AppendLine($"56 {y - 8} 483 22 re S");
        builder.AppendLine("0.08 0.10 0.12 rg");

        if (hasServeCostColumn)
        {
            DrawVerticalLine(builder, 114, y - 8, 22);
            DrawVerticalLine(builder, 310, y - 8, 22);
            DrawVerticalLine(builder, 360, y - 8, 22);
            DrawVerticalLine(builder, 418, y - 8, 22);
            DrawVerticalLine(builder, 488, y - 8, 22);
            WriteText(builder, "F1", 6, 62, y, Trim(line.Code.Replace("\\", "/", StringComparison.Ordinal), 10));
            WriteText(builder, "F1", 6, 120, y, Trim(line.Description, 35));
            WriteText(builder, "F1", 6, 318, y, Trim(line.Unit, 7));
            WriteText(builder, "F1", 6, 366, y, line.PricePerUnit);
            WriteText(builder, "F1", 6, 423, y, EmptyFallback(line.RtdPricePerLitre));
            WriteText(builder, "F1", 6, 493, y, EmptyFallback(line.ServeCost));
            return;
        }

        DrawVerticalLine(builder, 116, y - 8, 22);
        DrawVerticalLine(builder, 338, y - 8, 22);
        DrawVerticalLine(builder, 402, y - 8, 22);
        DrawVerticalLine(builder, 468, y - 8, 22);
        WriteText(builder, "F1", 7, 64, y, Trim(line.Code.Replace("\\", "/", StringComparison.Ordinal), 11));
        WriteText(builder, "F1", 7, 124, y, Trim(line.Description, 39));
        WriteText(builder, "F1", 7, 346, y, Trim(line.Unit, 8));
        WriteText(builder, "F1", 7, 410, y, line.PricePerUnit);
        WriteText(builder, "F1", 7, 474, y, EmptyFallback(line.RtdPricePerLitre));
    }

    private static void DrawImage(StringBuilder builder, string name, int x, int y, int width, int height)
    {
        builder.AppendLine("q");
        builder.AppendLine($"{width} 0 0 {height} {x} {y} cm");
        builder.AppendLine($"/{name} Do");
        builder.AppendLine("Q");
    }

    private static void DrawVerticalLine(StringBuilder builder, int x, int y, int height)
    {
        builder.AppendLine($"{x} {y} m {x} {y + height} l S");
    }

    private static void WriteText(StringBuilder builder, string font, int size, int x, int y, string text)
    {
        builder.AppendLine("BT");
        builder.AppendLine($"/{font} {size} Tf");
        builder.AppendLine($"{x} {y} Td");
        builder.AppendLine($"({Escape(text)}) Tj");
        builder.AppendLine("ET");
    }

    private static PdfImage? LoadLogo()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "simpsons-logo-blue.png");
        if (!File.Exists(path))
        {
            return null;
        }

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(path);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();

        var width = bitmap.PixelWidth;
        var height = bitmap.PixelHeight;
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));
            context.DrawImage(bitmap, new Rect(0, 0, width, height));
        }

        var render = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        render.Render(visual);

        var encoder = new JpegBitmapEncoder { QualityLevel = 95 };
        encoder.Frames.Add(BitmapFrame.Create(render));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        return new PdfImage(width, height, stream.ToArray());
    }

    private static void WritePdf(string path, IReadOnlyList<byte[]> objects)
    {
        using var stream = File.Create(path);
        stream.Write(Bytes("%PDF-1.4\n"));

        var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(stream.Position);
            stream.Write(Bytes($"{index + 1} 0 obj\n"));
            stream.Write(objects[index]);
            stream.Write(Bytes("\nendobj\n"));
        }

        var xrefPosition = stream.Position;
        stream.Write(Bytes($"xref\n0 {objects.Count + 1}\n"));
        stream.Write(Bytes("0000000000 65535 f \n"));

        foreach (var offset in offsets.Skip(1))
        {
            stream.Write(Bytes($"{offset:0000000000} 00000 n \n"));
        }

        stream.Write(Bytes("trailer\n"));
        stream.Write(Bytes($"<< /Size {objects.Count + 1} /Root 1 0 R >>\n"));
        stream.Write(Bytes("startxref\n"));
        stream.Write(Bytes($"{xrefPosition}\n"));
        stream.Write(Bytes("%%EOF"));
    }

    private static byte[] BuildImageObject(PdfImage image)
    {
        return BuildStreamObject(
            image.JpegBytes,
            $"<< /Type /XObject /Subtype /Image /Width {image.Width} /Height {image.Height} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length {image.JpegBytes.Length} >>");
    }

    private static byte[] BuildStreamObject(byte[] streamBytes, string? dictionary = null)
    {
        dictionary ??= $"<< /Length {streamBytes.Length} >>";
        using var stream = new MemoryStream();
        stream.Write(Bytes($"{dictionary}\nstream\n"));
        stream.Write(streamBytes);
        stream.Write(Bytes("\nendstream"));
        return stream.ToArray();
    }

    private static byte[] Bytes(string value)
    {
        return PdfEncoding.GetBytes(value);
    }

    private static string EmptyFallback(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }

    private static string Trim(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "-";
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : string.Concat(trimmed.AsSpan(0, maxLength - 3), "...");
    }

    private static string Escape(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal)
            .Replace("\u00A3", "\\243", StringComparison.Ordinal);
    }
}

internal sealed record PdfImage(int Width, int Height, byte[] JpegBytes);

public sealed record QuotePdfModel(
    string Customer,
    string UsagePriceHeader,
    bool HasServeCostColumn,
    string ServeCostHeader,
    IReadOnlyList<QuotePdfLine> Lines);

public sealed record QuotePdfLine(
    string Code,
    string Description,
    string Unit,
    string PricePerUnit,
    string RtdPricePerLitre,
    string ServeCost,
    string GrossProfit);
