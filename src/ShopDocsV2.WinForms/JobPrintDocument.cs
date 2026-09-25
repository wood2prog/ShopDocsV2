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
            if (line.IsSeparator)
            {
                var lineY = y + line.Height / 2;
                e.Graphics!.DrawLine(Pens.Black, e.MarginBounds.Left, lineY, e.MarginBounds.Right, lineY);
            }
            else
            {
                e.Graphics!.DrawString(line.Text, line.Font, Brushes.Black, e.MarginBounds.Left + line.IndentX, y);
            }
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
            _roomTitleFont.Dispose();
            _sectionTitleFont.Dispose();
            _normalFont.Dispose();
        }
        base.Dispose(disposing);
    }

    private List<PrintLine> BuildLines(Graphics measureGraphics, int contentWidth)
    {
        var lines = new List<PrintLine>();
        var block = -1; // -1 for the job header, then the room's index.

        void Add(string text, Font font, float indent, bool keepWithNext = false)
        {
            var height = measureGraphics.MeasureString(text.Length == 0 ? " " : text, font, Math.Max(1, contentWidth - (int)indent)).Height;
            lines.Add(new PrintLine(text, font, indent, keepWithNext, height, block));
        }

        Add(DocumentName, _titleFont, 0, keepWithNext: _spec.HeaderFields.Count > 0);
        foreach (var (label, value) in _spec.HeaderFields)
        {
            Add($"{label}: {value}", _normalFont, 0);
        }
        Add("", _normalFont, 0);

        for (var roomIndex = 0; roomIndex < _spec.Rooms.Count; roomIndex++)
        {
            var (roomName, sections) = _spec.Rooms[roomIndex];
            block = roomIndex;
            if (roomIndex > 0)
            {
                var separatorHeight = measureGraphics.MeasureString(" ", _normalFont).Height;
                lines.Add(new PrintLine("", _normalFont, 0, false, separatorHeight, block, IsSeparator: true));
            }

            Add(roomName, _roomTitleFont, 0, keepWithNext: true);

            if (sections.Count == 0)
            {
                Add(RoomSpecSection.NoDetailsText, _normalFont, 0);
            }

            foreach (var section in sections)
            {
                Add(section.Title, _sectionTitleFont, 0, keepWithNext: true);
                foreach (var line in section.Lines)
                {
                    var textLines = line.ToTextLines().ToList();
                    for (var i = 0; i < textLines.Count; i++)
                    {
                        var (text, isListItem) = textLines[i];
                        // A list's "Label:" line stays on the same page as its first row.
                        var keepWithNext = !isListItem && i + 1 < textLines.Count && textLines[i + 1].IsListItem;
                        Add(text, _normalFont, isListItem ? 20 : 0, keepWithNext);
                    }
                }
            }

            Add("", _normalFont, 0);
        }

        return lines;
    }

    /// <summary>
    /// Bin-packs lines into pages; a line marked KeepWithNext is pushed to the next page if it would otherwise land as the last line on this one.
    /// A room that doesn't fit in what's left of the page starts a new page, unless it's too tall for a page of its own, in which case it splits
    /// line by line. A room separator that doesn't fit is dropped rather than printed at the top of the next page, where the page break already
    /// separates the rooms.
    /// </summary>
    private static List<List<PrintLine>> Paginate(List<PrintLine> lines, int pageHeight)
    {
        var pages = new List<List<PrintLine>>();
        var currentPage = new List<PrintLine>();
        float currentHeight = 0;

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            if (currentPage.Count > 0 && (i == 0 || lines[i - 1].Block != line.Block))
            {
                var blockHeight = lines.Skip(i).TakeWhile(l => l.Block == line.Block).Sum(l => l.Height);
                var heightOnNewPage = blockHeight - (line.IsSeparator ? line.Height : 0);
                if (currentHeight + blockHeight > pageHeight && heightOnNewPage <= pageHeight)
                {
                    pages.Add(currentPage);
                    currentPage = [];
                    currentHeight = 0;
                    if (line.IsSeparator)
                    {
                        continue;
                    }
                }
            }

            var fits = currentHeight + line.Height <= pageHeight;

            if (fits && line.KeepWithNext && i + 1 < lines.Count &&
                currentHeight + line.Height + lines[i + 1].Height > pageHeight)
            {
                fits = false;
            }

            if (!fits && line.IsSeparator)
            {
                continue;
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

    private readonly record struct PrintLine(string Text, Font Font, float IndentX, bool KeepWithNext, float Height, int Block, bool IsSeparator = false);
}
