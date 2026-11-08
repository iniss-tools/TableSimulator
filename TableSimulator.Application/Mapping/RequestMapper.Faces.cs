using TableSimulator.Contracts.Faces;
using TableSimulator.Core.Boards.Faces;

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
                Contracts.Faces.FaceElementKind.Rect => Core.Boards.Faces.FaceElementKind.Rect,
                Contracts.Faces.FaceElementKind.Text => Core.Boards.Faces.FaceElementKind.Text,
                Contracts.Faces.FaceElementKind.Clock => Core.Boards.Faces.FaceElementKind.Clock,
                Contracts.Faces.FaceElementKind.Symbol => Core.Boards.Faces.FaceElementKind.Symbol,
                Contracts.Faces.FaceElementKind.Image => Core.Boards.Faces.FaceElementKind.Image,
                _ => throw new ArgumentOutOfRangeException(nameof(element), element.Kind, null)
            },
            Anchor = element.Anchor switch
            {
                Contracts.Faces.FaceAnchor.Face => Core.Boards.Faces.FaceAnchor.Face,
                Contracts.Faces.FaceAnchor.Matrix => Core.Boards.Faces.FaceAnchor.Matrix,
                Contracts.Faces.FaceAnchor.Row => Core.Boards.Faces.FaceAnchor.Row,
                Contracts.Faces.FaceAnchor.Top => Core.Boards.Faces.FaceAnchor.Top,
                Contracts.Faces.FaceAnchor.Bottom => Core.Boards.Faces.FaceAnchor.Bottom,
                Contracts.Faces.FaceAnchor.Left => Core.Boards.Faces.FaceAnchor.Left,
                Contracts.Faces.FaceAnchor.Right => Core.Boards.Faces.FaceAnchor.Right,
                _ => throw new ArgumentOutOfRangeException(nameof(element), element.Anchor, null)
            },
            Row = element.Row,
            X = element.X,
            Y = element.Y,
            Width = element.Width,
            Height = element.Height,
            Align = element.Align switch
            {
                Contracts.Faces.FaceAlign.Left => Core.Boards.Faces.FaceAlign.Left,
                Contracts.Faces.FaceAlign.Center => Core.Boards.Faces.FaceAlign.Center,
                Contracts.Faces.FaceAlign.Right => Core.Boards.Faces.FaceAlign.Right,
                _ => throw new ArgumentOutOfRangeException(nameof(element), element.Align, null)
            },
            VAlign = element.VAlign switch
            {
                Contracts.Faces.FaceVAlign.Top => Core.Boards.Faces.FaceVAlign.Top,
                Contracts.Faces.FaceVAlign.Middle => Core.Boards.Faces.FaceVAlign.Middle,
                Contracts.Faces.FaceVAlign.Bottom => Core.Boards.Faces.FaceVAlign.Bottom,
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
                Contracts.Faces.FaceClockFormat.HourMinute => Core.Boards.Faces.FaceClockFormat.HourMinute,
                Contracts.Faces.FaceClockFormat.HourMinutePadded => Core.Boards.Faces.FaceClockFormat.HourMinutePadded,
                Contracts.Faces.FaceClockFormat.WithSeconds => Core.Boards.Faces.FaceClockFormat.WithSeconds,
                _ => throw new ArgumentOutOfRangeException(nameof(element), element.Format, null)
            },
            Render = element.Render == Contracts.Faces.FaceRender.Display ? Core.Boards.Faces.FaceRender.Display : Core.Boards.Faces.FaceRender.Text,
            Symbol = ToSymbol(element.Symbol),
            // obrazok len pri prvku obrazok - inde by bol zbytocnou zatazou predlohy
            Image = element.Kind == Contracts.Faces.FaceElementKind.Image ? element.Image.Trim() : "",
            Locked = element.Locked
        };
    }

    private static Core.Boards.Faces.FaceSymbol ToSymbol(Contracts.Faces.FaceSymbol symbol) =>
        symbol switch
        {
            Contracts.Faces.FaceSymbol.ArrowUp => Core.Boards.Faces.FaceSymbol.ArrowUp,
            Contracts.Faces.FaceSymbol.ArrowDown => Core.Boards.Faces.FaceSymbol.ArrowDown,
            Contracts.Faces.FaceSymbol.ArrowLeft => Core.Boards.Faces.FaceSymbol.ArrowLeft,
            Contracts.Faces.FaceSymbol.ArrowRight => Core.Boards.Faces.FaceSymbol.ArrowRight,
            Contracts.Faces.FaceSymbol.ArrowUpLeft => Core.Boards.Faces.FaceSymbol.ArrowUpLeft,
            Contracts.Faces.FaceSymbol.ArrowUpRight => Core.Boards.Faces.FaceSymbol.ArrowUpRight,
            Contracts.Faces.FaceSymbol.ArrowDownLeft => Core.Boards.Faces.FaceSymbol.ArrowDownLeft,
            Contracts.Faces.FaceSymbol.ArrowDownRight => Core.Boards.Faces.FaceSymbol.ArrowDownRight,
            Contracts.Faces.FaceSymbol.ArrowsLeftRight => Core.Boards.Faces.FaceSymbol.ArrowsLeftRight,
            Contracts.Faces.FaceSymbol.ArrowsUpDown => Core.Boards.Faces.FaceSymbol.ArrowsUpDown,
            _ => throw new ArgumentOutOfRangeException(nameof(symbol), symbol, null)
        };
}
