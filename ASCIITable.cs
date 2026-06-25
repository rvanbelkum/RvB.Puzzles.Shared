using System.Collections;
using System.ComponentModel;
using static RvB.Puzzles.Shared.Display;
using static System.Console;

namespace RvB.Puzzles.Shared;

public enum Alignment {
    Left,
    Center,
    Right,
}

public enum BorderStyle {
    Single,
    Double,
    DoubleOutside
}

public enum ListStyle {
    TopToBottom,
    LeftToRight,
    TopToBottomNumbered,
    TopToBottomNumberedHeader,
    LeftToRightNumbered,
}

public class ASCIITableColumn {
    internal int? _declaredWidth;
    internal int? _calculatedWidth;

    public ASCIITableColumn() { }

    public ASCIITableColumn(string title) {
        Title = title;
    }

    public string? Title { get; init; } = null;

    public Alignment? HeaderAlignment { get; init; } = null;

    public ConsoleColor? HeaderColor { get; init; } = null;

    public int? Width { get => _calculatedWidth; init => _declaredWidth = _calculatedWidth = value; }

    public ConsoleColor? Color { get; init; } = null;

    public Alignment? Alignment { get; init; } = null;
}

public class ASCIITableOptions : IEnumerable<(ASCIITableColumn Column, int Index)> {
    public const int DefaultHorizontalPadding = 1;

    private readonly ASCIITableColumn[] _columns;

    public ASCIITableOptions(int columnCount) {
        _columns = Enumerable.Range(0, columnCount).Select(_ => new ASCIITableColumn()).ToArray();
    }

    public ASCIITableOptions(ASCIITableColumn[] columns) {
        _columns = columns;
    }

    public int ColumnCount => _columns.Length;

    public bool DrawHeader { get; init; } = true;

    public int DefaultColumnWidth { get; init; } = 8;

    public BorderStyle BorderStyle { get; init; } = BorderStyle.Single;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete($"HeaderColor is deprecated, please use {nameof(DefaultHeaderColor)} instead.")]
    public ConsoleColor HeaderColor {
        get => DefaultHeaderColor;
        init => DefaultHeaderColor = value;
    }

    public ConsoleColor BorderColor { get; init; } = Console.ForegroundColor;

    public int HorizontalPadding { get; init; } = DefaultHorizontalPadding;

    public ConsoleColor DefaultColumnColor { get; init; } = Console.ForegroundColor;

    public Alignment DefaultHeaderAlignment { get; init; } = Alignment.Center;

    public ConsoleColor DefaultHeaderColor { get; init; } = Console.ForegroundColor;

    public Alignment DefaultColumnAlignment { get; init; } = Alignment.Left;

    public IEnumerator<(ASCIITableColumn Column, int Index)> GetEnumerator() {
        for (var col = 0; col < _columns.Length; col += 1) {
            yield return (_columns[col], col);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static string ColumnTitle(ASCIITableColumn column)
        => column.Title ?? string.Empty;

    public Alignment HeaderAlignment(ASCIITableColumn column)
        => column.HeaderAlignment ?? DefaultHeaderAlignment;

    public Alignment ColumnAlignment(ASCIITableColumn column)
        => column.Alignment ?? DefaultColumnAlignment;

    public int ColumnWidth(ASCIITableColumn column)
        => column.Width ?? DefaultColumnWidth;

    public ConsoleColor ColumnColor(ASCIITableColumn column)
        => column.Color ?? DefaultColumnColor;
}

public class ASCIITableListOptions {
    public enum ColumnCountMode {
        Ignore,
        Exact,
        Max,
        Min
    }

    public static (int, ColumnCountMode) Exactly(int columnCount) {
        return (columnCount, ColumnCountMode.Exact);
    }

    public static (int, ColumnCountMode) AtLeast(int columnCount) {
        return (columnCount, ColumnCountMode.Min);
    }

    public static (int, ColumnCountMode) AtMost(int columnCount) {
        return (columnCount, ColumnCountMode.Max);
    }

    public (int Count, ColumnCountMode Mode) Columns { get; set; } = (0, ColumnCountMode.Ignore);

    //public int ColumnCount { get; set; }

    //public ColumnCountMode Mode { get; set; } = ColumnCountMode.Ignore;

    public bool NumberedItems { get; set; } = false;

    public bool RangeInHeader { get; set; } = false;
}

public class ASCIITable {
    private readonly ASCIITableOptions _options;
    private readonly bool _keepPosition;
    private readonly string _horizontalPadding;
    private bool _drawnHeader = false;
    private Borders _borders;
    private int? _left, _top;

    private struct Border {
        public char Left;
        public char Inner;
        public char Right;
        public char Horz;

        public Border(char left, char inner, char right, char horz) {
            Left = left;
            Inner = inner;
            Right = right;
            Horz = horz;
        }
    }

    private struct Borders {
        public Border TopBorder;
        public Border InnerBorder;
        public Border BottomBorder;
        public Border DataBorder;
    }

    private static readonly Dictionary<BorderStyle, Borders> s_borders = new() {
        [BorderStyle.Single] = new() {
            TopBorder = new('┌', '┬', '┐', '─'),
            InnerBorder = new('├', '┼', '┤', '─'),
            BottomBorder = new('└', '┴', '┘', '─'),
            DataBorder = new('│', '│', '│', ' ')
        },
        [BorderStyle.Double] = new() {
            TopBorder = new('╔', '╦', '╗', '═'),
            InnerBorder = new('╠', '╬', '╣', '═'),
            BottomBorder = new('╚', '╩', '╝', '═'),
            DataBorder = new('║', '║', '║', ' ')
        },
        [BorderStyle.DoubleOutside] = new() {
            TopBorder = new('╔', '╤', '╗', '═'),
            InnerBorder = new('╟', '┼', '╢', '─'),
            BottomBorder = new('╚', '╧', '╝', '═'),
            DataBorder = new('║', '│', '║', ' ')
        }
    };

    public ASCIITable(ASCIITableOptions options, bool keepPosition = false) {
        _options = options;
        _keepPosition = keepPosition;
        _borders = s_borders[_options.BorderStyle];
        _horizontalPadding = new string(' ', options.HorizontalPadding);
    }

    public void Draw(IEnumerable<string[]> rows) {
        if (_keepPosition) {
            if (_left is null || _top is null) {
                (_left, _top) = GetCursorPosition();
            } else {
                SetCursorPosition(_left.Value, _top.Value);
            }
        }
        foreach (var (column, columnIndex) in _options) {
            if (column._declaredWidth == null) {
                var width = Math.Max(1, ASCIITableOptions.ColumnTitle(column).Length);
                foreach (var row in rows) {
                    if (columnIndex < row.Length && row[columnIndex] != null) {
                        width = Math.Max(width, row[columnIndex].Length);
                    }
                }
                column._calculatedWidth = width;
            }
        }
        DrawHeader();
        foreach (var row in rows) {
            DrawRow(row);
        }
        DrawFooter();
    }

    public void DrawHeader() {
        DrawHorizontalBorder(_borders.TopBorder);
        if (_options.DrawHeader) {
            Write(_borders.DataBorder.Left, _options.BorderColor);
            Write(_horizontalPadding);
            foreach (var (column, columnIndex) in _options) {
                var header = Align(ASCIITableOptions.ColumnTitle(column), _options.ColumnWidth(column), _options.HeaderAlignment(column));
                Write(header, column.HeaderColor ?? _options.DefaultHeaderColor);
                if (columnIndex < _options.ColumnCount - 1) {
                    Write(_horizontalPadding);
                    Write(_borders.DataBorder.Inner, _options.BorderColor);
                    Write(_horizontalPadding);
                }
            }
            Write(_horizontalPadding);
            WriteLine(_borders.DataBorder.Right, _options.BorderColor);
            DrawHorizontalBorder(_borders.InnerBorder);
        }
        _drawnHeader = true;
    }

    public void DrawRow(string[] cells) {
        if (cells.Length != _options.ColumnCount) {
            throw new ArgumentException("Wrong number of cells", nameof(cells));
        }
        if (!_drawnHeader) {
            DrawHeader();
        }
        Write(_borders.DataBorder.Left, _options.BorderColor);
        Write(_horizontalPadding);
        foreach (var (column, columnIndex) in _options) {
            var cell = Align(cells[columnIndex], _options.ColumnWidth(column), _options.ColumnAlignment(column));
            Write(cell, _options.ColumnColor(column));
            if (columnIndex < cells.Length - 1) {
                Write(_horizontalPadding);
                Write(_borders.DataBorder.Inner, _options.BorderColor);
                Write(_horizontalPadding);
            }
        }
        Write(_horizontalPadding);
        WriteLine(_borders.DataBorder.Right, _options.BorderColor);
    }

    public void DrawFooter() {
        DrawHorizontalBorder(_borders.BottomBorder);
    }

    public static void DrawTable(ASCIITableOptions options, IEnumerable<string[]> rows) {
        var table = new ASCIITable(options);
        table.Draw(rows);
    }

    private const string NumberPostfix = ". ";

    private static int CalcColumnCount(List<string> list, ASCIITableListOptions listOptions, Func<int, int> GetTotalColumnWidth) {
        if (listOptions.Columns.Mode == ASCIITableListOptions.ColumnCountMode.Exact) {
            return listOptions.Columns.Count;
        }
        int minColumnCount, maxColumnCount;
        if (listOptions.Columns.Mode == ASCIITableListOptions.ColumnCountMode.Min) {
            minColumnCount = listOptions.Columns.Count;
            maxColumnCount = int.MaxValue;
        } else if (listOptions.Columns.Mode == ASCIITableListOptions.ColumnCountMode.Max) {
            minColumnCount = 1;
            maxColumnCount = listOptions.Columns.Count;
        } else {
            minColumnCount = 1;
            maxColumnCount = int.MaxValue;
        }
        var columnCount = minColumnCount;
        while (columnCount < list.Count && columnCount < maxColumnCount) {
            var nextColumnCount = columnCount + 1;
            var totalColumnWidth = GetTotalColumnWidth(nextColumnCount);
            // nextColumnCount + 1 => number of borders (|)
            // 2 * ASCIITableOptions.DefaultHorizontalPadding * nextColumnCount => total column padding
            var totalWidth = nextColumnCount + 1 + 2 * ASCIITableOptions.DefaultHorizontalPadding * nextColumnCount + totalColumnWidth;
            if (totalWidth > WindowWidth) {
                break;
            }
            columnCount = nextColumnCount;
        }
        var rowCount1 = (list.Count + columnCount - 1) / columnCount;
        columnCount = int.Min(columnCount, 1 + (list.Count - 1) / rowCount1);
        columnCount = int.Max(minColumnCount, columnCount);
        columnCount = int.Min(maxColumnCount, columnCount);
        return columnCount;
    }

    public static void DrawListAsTableTopDown(List<string> list, ASCIITableListOptions listOptions) {
        const string RangeSeparator = " - ";
        const string EmptyRange = "-";

        if (listOptions.Columns.Mode != ASCIITableListOptions.ColumnCountMode.Ignore) {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(listOptions.Columns.Count);
        }
        if (list == null || list.Count == 0) {
            return;
        }
        var columnCount = CalcColumnCount(list, listOptions, GetTotalColumnWidth);
        var rowCount = (list.Count + columnCount - 1) / columnCount;

        var rows = new string[rowCount][];
        for (int r = 0; r < rowCount; r++) {
            rows[r] = new string[columnCount];
        }
        ASCIITableColumn[]? header = null;
        if (listOptions.RangeInHeader) {
            header = new ASCIITableColumn[columnCount];
            for (var c = 0; c < columnCount; c++) {
                var start = 1 + c * rowCount;
                var end = int.Min((c + 1) * rowCount, list.Count);
                if (start <= end) {
                    header[c] = new($"{start}{RangeSeparator}{end}");
                } else {
                    header[c] = new(EmptyRange);
                }
            }
        }
        var index = 0;
        for (var c = 0; c < columnCount; c++) {
            var indexWidth = 1 + (int)Math.Log10(index + rowCount); // last number in the column
            for (int r = 0; r < rowCount && index < list.Count; r++) {
                string item = string.Empty;
                if (listOptions.NumberedItems) {
                    item = $"{(index + 1).ToString().PadLeft(indexWidth)}{NumberPostfix}";
                }
                item += list[index];
                rows[r][c] = item;
                index += 1;
            }
        }
        ASCIITableOptions tableOptions;
        if (header != null) {
            tableOptions = new(header) { BorderStyle = BorderStyle.DoubleOutside };
        } else {
            tableOptions = new(columnCount) { DrawHeader = false };
        }
        DrawTable(tableOptions, rows);

        int GetTotalColumnWidth(int columnCount) {
            var rowCount = (list.Count + columnCount - 1) / columnCount;
            var index = 0;
            var totalWidth = 0;
            for (var c = 0; c < columnCount; c++) {
                var columnWidth = 0;
                var first = 1 + index;                                 // first number in the column
                var last = int.Min(first + rowCount - 1, list.Count); // last number in the column
                var numberWidthLast = 1 + (int)Math.Log10(last);
                if (listOptions.RangeInHeader) {
                    if (first <= last) {
                        var numberWidthFirst = 1 + (int)Math.Log10(first);
                        columnWidth = numberWidthFirst + RangeSeparator.Length + numberWidthLast;
                    } else {
                        columnWidth = 1;
                    }
                }
                for (int r = 0; r < rowCount && index < list.Count; r++) {
                    var cellWidth = 0;
                    if (listOptions.NumberedItems) {
                        cellWidth += numberWidthLast + NumberPostfix.Length;
                    }
                    cellWidth += list[index].Length;
                    columnWidth = int.Max(columnWidth, cellWidth);
                    index += 1;
                }
                totalWidth += columnWidth;
            }
            return totalWidth;
        }
    }

    public static void DrawListAsTableLeftToRight(List<string> list, ASCIITableListOptions listOptions) {
        if (listOptions.Columns.Mode != ASCIITableListOptions.ColumnCountMode.Ignore) {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(listOptions.Columns.Count);
        }
        if (list == null || list.Count == 0) {
            return;
        }
        var columnCount = CalcColumnCount(list, listOptions, GetTotalColumnWidth);
        var rowCount = (list.Count + columnCount - 1) / columnCount;

        var rows = new string[rowCount][];
        for (int r = 0; r < rowCount; r++) {
            rows[r] = new string[columnCount];
        }
        for (var c = 0; c < columnCount; c++) {
            var index = c;
            var first = 1 + index;                                 // first number in the column
            var last = first + (rowCount - 1) * columnCount;
            while (last > list.Count) {
                last -= columnCount;
            }
            var numberWidthLast = (last < first) ? 0 : 1 + (int)Math.Log10(last);
            for (int r = 0; r < rowCount && index < list.Count; r++) {
                string item = string.Empty;
                if (listOptions.NumberedItems) {
                    item = $"{(index + 1).ToString().PadLeft(numberWidthLast)}{NumberPostfix}";
                }
                item += list[index];
                rows[r][c] = item;
                index += columnCount;
            }
        }
        ASCIITableOptions options = new(columnCount) { DrawHeader = false };
        DrawTable(options, rows);

        int GetTotalColumnWidth(int columnCount) {
            var rowCount = (list.Count + columnCount - 1) / columnCount;
            var totalWidth = 0;
            for (var c = 0; c < columnCount; c++) {
                var index = c;
                var columnWidth = 0;
                var first = 1 + index;                                 // first number in the column
                var last = first + (rowCount - 1) * columnCount;
                while (last > list.Count) {
                    last -= columnCount;
                }
                var numberWidthLast = (last < first) ? 0 : 1 + (int)Math.Log10(last);
                //if (listOptions.RangeInHeader) {
                //    if (first <= last) {
                //        var numberWidthFirst = 1 + (int)Math.Log10(first);
                //        columnWidth = numberWidthFirst + RangeSeparator.Length + numberWidthLast;
                //    } else {
                //        columnWidth = 1;
                //    }
                //}
                for (int r = 0; r < rowCount && index < list.Count; r++) {
                    var cellWidth = 0;
                    if (listOptions.NumberedItems) {
                        cellWidth += numberWidthLast + NumberPostfix.Length;
                    }
                    cellWidth += list[index].Length;
                    columnWidth = int.Max(columnWidth, cellWidth);
                    index += columnCount;
                }
                totalWidth += columnWidth;
            }
            return totalWidth;
        }
    }

    private string Align(string text, int width, Alignment alignment) {
        if (text is null) {
            return new string(' ', width);
        }
        if (text.Length > width) {
            return text[..width];
        }
        if (text.Length < width) {
            return alignment switch {
                Alignment.Left => text.PadRight(width, _borders.DataBorder.Horz),
                Alignment.Center => text.PadLeft(text.Length + (width - text.Length) / 2, _borders.DataBorder.Horz).PadRight(width, _borders.DataBorder.Horz),
                Alignment.Right => text.PadLeft(width, _borders.DataBorder.Horz),
                _ => throw new NotImplementedException(),
            };
        }
        return text;
    }

    private void DrawHorizontalBorder(Border border) {
        Write(border.Left, _options.BorderColor);
        foreach (var (column, columnIndex) in _options) {
            var width = _options.ColumnWidth(column);
            Write(new string(border.Horz, width + 2 * _options.HorizontalPadding), _options.BorderColor);
            if (columnIndex < _options.ColumnCount - 1) {
                Write(border.Inner, _options.BorderColor);
            }
        }
        Write(border.Right, _options.BorderColor);
        WriteLine();
    }
}
