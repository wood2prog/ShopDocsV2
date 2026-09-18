using System.Drawing.Printing;
using ShopDocsV2.Application;

namespace ShopDocsV2.WinForms;

/// <summary>
/// Renders a PrintableJobSpec. Pagination is computed once, on the first PrintPage call (using that
/// call's real Graphics/MarginBounds, so measurement and drawing agree on scale), then bin-packed into
/// pages up front rather than recomputed live on every page, so a header never gets orphaned alone at
/// the bottom of a page.
/// </summary>
internal sealed class JobPrintDocument : PrintDocument
{
    private readonly PrintableJobSpec _spec;
    private readonly Font _titleFont = new("Segoe UI", 16, FontStyle.Bold);
    private readonly Font _headerFont = new("Segoe UI", 10);
    private readonly Font _roomTitleFont = new("Segoe UI", 14, FontStyle.Bold);
    private readonly Font _sectionTitleFont = new("Segoe UI", 11, FontStyle.Bold);
    private readonly Font _normalFont = new("Segoe UI", 10);

    private List<List<PrintLine>>? _pages;
    private int _pageIndex;

    public JobPrintDocument(PrintableJobSpec spec, string documentName)
    {
        _spec = spec;
        DocumentName = documentName;
    }

    protected override void OnBeginPrint(PrintEventArgs e)
    {
        base.OnBeginPrint(e);
        _pageIndex = 0;
        _pages = null;
    }

    protected override void OnPrintPage(PrintPageEventArgs e)
    {
        base.OnPrintPage(e);

        if (_pages is null)
        {
            var lines = BuildLines(e.Graphics!, e.MarginBounds.Width);
            _pages = Paginate(lines, e.MarginBounds.Height);
        }

        var page = _pages[_pageIndex];
        var y = (float)e.MarginBounds.Top;
        foreach (var line in page)
        {
            e.Graphics!.DrawString(line.Text, line.Font, Brushes.Black, e.MarginBounds.Left + line.IndentX, y);
            y += line.Height;
        }

        _pageIndex++;
        e.HasMorePages = _pageIndex < _pages.Count;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _titleFont.Dispose();
            _headerFont.Dispose();
            _roomTitleFont.Dispose();
            _sectionTitleFont.Dispose();
            _normalFont.Dispose();
        }
        base.Dispose(disposing);
    }

    private List<PrintLine> BuildLines(Graphics measureGraphics, int contentWidth)
    {
        var lines = new List<PrintLine>();

        void Add(string text, Font font, float indent, bool keepWithNext = false)
        {
            var height = measureGraphics.MeasureString(text.Length == 0 ? " " : text, font, Math.Max(1, contentWidth - (int)indent)).Height;
            lines.Add(new PrintLine(text, font, indent, keepWithNext, height));
        }

        Add(DocumentName, _titleFont, 0, keepWithNext: _spec.HeaderFields.Count > 0);
        foreach (var (label, value) in _spec.HeaderFields)
        {
            Add($"{label}: {value}", _headerFont, 0);
        }
        Add("", _headerFont, 0);

        foreach (var (roomName, sections) in _spec.Rooms)
        {
            Add(roomName, _roomTitleFont, 0, keepWithNext: true);

            if (sections.Count == 0)
            {
                Add("No details entered for this room.", _normalFont, 0);
            }

            foreach (var section in sections)
            {
                Add(section.Title, _sectionTitleFont, 0, keepWithNext: true);
                foreach (var line in section.Lines)
                {
                    if (line.IsList)
                    {
                        if (line.ShowLabel)
                        {
                            Add($"{line.Label}:", _normalFont, 0, keepWithNext: true);
                        }
                        foreach (var listLine in line.ListLines ?? [])
                        {
                            Add(listLine, _normalFont, 20);
                        }
                    }
                    else
                    {
                        Add(line.ShowLabel ? $"{line.Label}: {line.Value}" : line.Value ?? "", _normalFont, 0);
                    }
                }
            }

            Add("", _normalFont, 0);
        }

        return lines;
    }

    /// <summary>Bin-packs lines into pages; a line marked KeepWithNext is pushed to the next page if it would otherwise land as the last line on this one.</summary>
    private static List<List<PrintLine>> Paginate(List<PrintLine> lines, int pageHeight)
    {
        var pages = new List<List<PrintLine>>();
        var currentPage = new List<PrintLine>();
        float currentHeight = 0;

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var fits = currentHeight + line.Height <= pageHeight;

            if (fits && line.KeepWithNext && i + 1 < lines.Count &&
                currentHeight + line.Height + lines[i + 1].Height > pageHeight)
            {
                fits = false;
            }

            if (!fits && currentPage.Count > 0)
            {
                pages.Add(currentPage);
                currentPage = [];
                currentHeight = 0;
            }

            currentPage.Add(line);
            currentHeight += line.Height;
        }

        if (currentPage.Count > 0)
        {
            pages.Add(currentPage);
        }

        return pages.Count > 0 ? pages : [[]];
    }

    private readonly record struct PrintLine(string Text, Font Font, float IndentX, bool KeepWithNext, float Height);
}
