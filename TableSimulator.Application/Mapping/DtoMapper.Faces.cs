using TableSimulator.Contracts.Faces;
using TableSimulator.Core.Boards.Faces;

namespace TableSimulator.Application.Mapping;

public static partial class DtoMapper
{
    public static BoardFaceDto ToDto(BoardFace face, bool builtIn)
    {
        ArgumentNullException.ThrowIfNull(face);
        return new BoardFaceDto
        {
            Id = face.Id,
            Name = face.Name,
            BuiltIn = builtIn,
            Margin = new FaceMarginDto(face.Margin.Left, face.Margin.Top, face.Margin.Right, face.Margin.Bottom),
            RowGap = face.RowGap,
            RowGroup = face.RowGroup,
            GroupGap = face.GroupGap,
            ModuleWidth = face.ModuleWidth,
            Elements = face.Elements.Select(ToDto).ToArray()
        };
    }

    public static FaceElementDto ToDto(FaceElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return new FaceElementDto
        {
            Kind = element.Kind switch
            {
                Core.Boards.Faces.FaceElementKind.Rect => Contracts.Faces.FaceElementKind.Rect,
                Core.Boards.Faces.FaceElementKind.Text => Contracts.Faces.FaceElementKind.Text,
                Core.Boards.Faces.FaceElementKind.Clock => Contracts.Faces.FaceElementKind.Clock,
                Core.Boards.Faces.FaceElementKind.Symbol => Contracts.Faces.FaceElementKind.Symbol,
                Core.Boards.Faces.FaceElementKind.Image => Contracts.Faces.FaceElementKind.Image,
                _ => throw new ArgumentOutOfRangeException(nameof(element), element.Kind, null)
            },
            Anchor = element.Anchor switch
            {
                Core.Boards.Faces.FaceAnchor.Face => Contracts.Faces.FaceAnchor.Face,
                Core.Boards.Faces.FaceAnchor.Matrix => Contracts.Faces.FaceAnchor.Matrix,
                Core.Boards.Faces.FaceAnchor.Row => Contracts.Faces.FaceAnchor.Row,
                Core.Boards.Faces.FaceAnchor.Top => Contracts.Faces.FaceAnchor.Top,
                Core.Boards.Faces.FaceAnchor.Bottom => Contracts.Faces.FaceAnchor.Bottom,
                Core.Boards.Faces.FaceAnchor.Left => Contracts.Faces.FaceAnchor.Left,
                Core.Boards.Faces.FaceAnchor.Right => Contracts.Faces.FaceAnchor.Right,
                _ => throw new ArgumentOutOfRangeException(nameof(element), element.Anchor, null)
            },
            Row = element.Row,
            X = element.X,
            Y = element.Y,
            Width = element.Width,
            Height = element.Height,
            Align = element.Align switch
            {
                Core.Boards.Faces.FaceAlign.Left => Contracts.Faces.FaceAlign.Left,
                Core.Boards.Faces.FaceAlign.Center => Contracts.Faces.FaceAlign.Center,
                Core.Boards.Faces.FaceAlign.Right => Contracts.Faces.FaceAlign.Right,
                _ => throw new ArgumentOutOfRangeException(nameof(element), element.Align, null)
            },
            VAlign = element.VAlign switch
            {
                Core.Boards.Faces.FaceVAlign.Top => Contracts.Faces.FaceVAlign.Top,
                Core.Boards.Faces.FaceVAlign.Middle => Contracts.Faces.FaceVAlign.Middle,
                Core.Boards.Faces.FaceVAlign.Bottom => Contracts.Faces.FaceVAlign.Bottom,
                _ => throw new ArgumentOutOfRangeException(nameof(element), element.VAlign, null)
            },
            Text = element.Text,
            Size = element.Size,
            Size2 = element.Size2,
            Bold = element.Bold,
            Style = element.Style,
            Color = element.Color,
            Fill = element.Fill,
            Border = element.Border,
            Radius = element.Radius,
            MaxWidth = element.MaxWidth,
            Spacing = element.Spacing,
            Format = element.Format switch
            {
                Core.Boards.Faces.FaceClockFormat.HourMinute => Contracts.Faces.FaceClockFormat.HourMinute,
                Core.Boards.Faces.FaceClockFormat.HourMinutePadded => Contracts.Faces.FaceClockFormat.HourMinutePadded,
                Core.Boards.Faces.FaceClockFormat.WithSeconds => Contracts.Faces.FaceClockFormat.WithSeconds,
                _ => throw new ArgumentOutOfRangeException(nameof(element), element.Format, null)
            },
            Render = element.Render == Core.Boards.Faces.FaceRender.Display ? Contracts.Faces.FaceRender.Display : Contracts.Faces.FaceRender.Text,
            Symbol = ToDto(element.Symbol),
            Image = element.Image,
            Locked = element.Locked
        };
    }

    private static Contracts.Faces.FaceSymbol ToDto(Core.Boards.Faces.FaceSymbol symbol) =>
        symbol switch
        {
            Core.Boards.Faces.FaceSymbol.ArrowUp => Contracts.Faces.FaceSymbol.ArrowUp,
            Core.Boards.Faces.FaceSymbol.ArrowDown => Contracts.Faces.FaceSymbol.ArrowDown,
            Core.Boards.Faces.FaceSymbol.ArrowLeft => Contracts.Faces.FaceSymbol.ArrowLeft,
            Core.Boards.Faces.FaceSymbol.ArrowRight => Contracts.Faces.FaceSymbol.ArrowRight,
            Core.Boards.Faces.FaceSymbol.ArrowUpLeft => Contracts.Faces.FaceSymbol.ArrowUpLeft,
            Core.Boards.Faces.FaceSymbol.ArrowUpRight => Contracts.Faces.FaceSymbol.ArrowUpRight,
            Core.Boards.Faces.FaceSymbol.ArrowDownLeft => Contracts.Faces.FaceSymbol.ArrowDownLeft,
            Core.Boards.Faces.FaceSymbol.ArrowDownRight => Contracts.Faces.FaceSymbol.ArrowDownRight,
            Core.Boards.Faces.FaceSymbol.ArrowsLeftRight => Contracts.Faces.FaceSymbol.ArrowsLeftRight,
            Core.Boards.Faces.FaceSymbol.ArrowsUpDown => Contracts.Faces.FaceSymbol.ArrowsUpDown,
            _ => throw new ArgumentOutOfRangeException(nameof(symbol), symbol, null)
        };
}
