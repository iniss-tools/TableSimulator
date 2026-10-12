using TableSimulator.Contracts.Faces;
using TableSimulator.Core.Boards;

namespace TableSimulator.Application.Mapping;

public static partial class RequestMapper
{
    /// <summary>Predloha cela z poziadavky; kluc z adresy poziadavky, nazov a texty bez okrajovych medzier.</summary>
    public static BoardFace ToFace(BoardFaceDto face, string id)
    {
        ArgumentNullException.ThrowIfNull(face);
        return new BoardFace
        {
            Id = id,
            Name = face.Name.Trim(),
            Margin = new FaceMargin(face.Margin.Left, face.Margin.Top, face.Margin.Right, face.Margin.Bottom),
            RowGap = face.RowGap,
            RowGroup = face.RowGroup,
            GroupGap = face.GroupGap,
            ModuleWidth = face.ModuleWidth,
            Elements = face.Elements.Select(ToElement).ToArray()
        };
    }

    public static FaceElement ToElement(FaceElementDto element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return new FaceElement
        {
            Kind = element.Kind switch
            {
                Contracts.Faces.FaceElementKind.Rect => Core.Boards.FaceElementKind.Rect,
                Contracts.Faces.FaceElementKind.Text => Core.Boards.FaceElementKind.Text,
                Contracts.Faces.FaceElementKind.Clock => Core.Boards.FaceElementKind.Clock,
                Contracts.Faces.FaceElementKind.Symbol => Core.Boards.FaceElementKind.Symbol,
                Contracts.Faces.FaceElementKind.Image => Core.Boards.FaceElementKind.Image,
                _ => throw new ArgumentOutOfRangeException(nameof(element), element.Kind, null)
            },
            Anchor = element.Anchor switch
            {
                Contracts.Faces.FaceAnchor.Face => Core.Boards.FaceAnchor.Face,
                Contracts.Faces.FaceAnchor.Matrix => Core.Boards.FaceAnchor.Matrix,
                Contracts.Faces.FaceAnchor.Row => Core.Boards.FaceAnchor.Row,
                Contracts.Faces.FaceAnchor.Top => Core.Boards.FaceAnchor.Top,
                Contracts.Faces.FaceAnchor.Bottom => Core.Boards.FaceAnchor.Bottom,
                Contracts.Faces.FaceAnchor.Left => Core.Boards.FaceAnchor.Left,
                Contracts.Faces.FaceAnchor.Right => Core.Boards.FaceAnchor.Right,
                _ => throw new ArgumentOutOfRangeException(nameof(element), element.Anchor, null)
            },
            Row = element.Row,
            X = element.X,
            Y = element.Y,
            Width = element.Width,
            Height = element.Height,
            Align = element.Align switch
            {
                Contracts.Faces.FaceAlign.Left => Core.Boards.FaceAlign.Left,
                Contracts.Faces.FaceAlign.Center => Core.Boards.FaceAlign.Center,
                Contracts.Faces.FaceAlign.Right => Core.Boards.FaceAlign.Right,
                _ => throw new ArgumentOutOfRangeException(nameof(element), element.Align, null)
            },
            VAlign = element.VAlign switch
            {
                Contracts.Faces.FaceVAlign.Top => Core.Boards.FaceVAlign.Top,
                Contracts.Faces.FaceVAlign.Middle => Core.Boards.FaceVAlign.Middle,
                Contracts.Faces.FaceVAlign.Bottom => Core.Boards.FaceVAlign.Bottom,
                _ => throw new ArgumentOutOfRangeException(nameof(element), element.VAlign, null)
            },
            Text = element.Text,
            Size = element.Size,
            Size2 = element.Size2,
            Bold = element.Bold,
            Style = element.Style.Trim(),
            Color = element.Color.Trim(),
            Fill = element.Fill.Trim(),
            Border = element.Border,
            Radius = element.Radius,
            MaxWidth = element.MaxWidth,
            Spacing = element.Spacing,
            Format = element.Format switch
            {
                Contracts.Faces.FaceClockFormat.HourMinute => Core.Boards.FaceClockFormat.HourMinute,
                Contracts.Faces.FaceClockFormat.HourMinutePadded => Core.Boards.FaceClockFormat.HourMinutePadded,
                Contracts.Faces.FaceClockFormat.WithSeconds => Core.Boards.FaceClockFormat.WithSeconds,
                _ => throw new ArgumentOutOfRangeException(nameof(element), element.Format, null)
            },
            Render = element.Render == Contracts.Faces.FaceRender.Display ? Core.Boards.FaceRender.Display : Core.Boards.FaceRender.Text,
            Symbol = ToSymbol(element.Symbol),
            // obrazok len pri prvku obrazok - inde by bol zbytocnou zatazou predlohy
            Image = element.Kind == Contracts.Faces.FaceElementKind.Image ? element.Image.Trim() : ""
        };
    }

    private static Core.Boards.FaceSymbol ToSymbol(Contracts.Faces.FaceSymbol symbol) =>
        symbol switch
        {
            Contracts.Faces.FaceSymbol.ArrowUp => Core.Boards.FaceSymbol.ArrowUp,
            Contracts.Faces.FaceSymbol.ArrowDown => Core.Boards.FaceSymbol.ArrowDown,
            Contracts.Faces.FaceSymbol.ArrowLeft => Core.Boards.FaceSymbol.ArrowLeft,
            Contracts.Faces.FaceSymbol.ArrowRight => Core.Boards.FaceSymbol.ArrowRight,
            Contracts.Faces.FaceSymbol.ArrowUpLeft => Core.Boards.FaceSymbol.ArrowUpLeft,
            Contracts.Faces.FaceSymbol.ArrowUpRight => Core.Boards.FaceSymbol.ArrowUpRight,
            Contracts.Faces.FaceSymbol.ArrowDownLeft => Core.Boards.FaceSymbol.ArrowDownLeft,
            Contracts.Faces.FaceSymbol.ArrowDownRight => Core.Boards.FaceSymbol.ArrowDownRight,
            Contracts.Faces.FaceSymbol.ArrowsLeftRight => Core.Boards.FaceSymbol.ArrowsLeftRight,
            Contracts.Faces.FaceSymbol.ArrowsUpDown => Core.Boards.FaceSymbol.ArrowsUpDown,
            _ => throw new ArgumentOutOfRangeException(nameof(symbol), symbol, null)
        };
}
