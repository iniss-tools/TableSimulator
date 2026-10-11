using TableSimulator.Contracts.Faces;
using TableSimulator.Core.Boards;

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
                Core.Boards.FaceElementKind.Rect => Contracts.Faces.FaceElementKind.Rect,
                Core.Boards.FaceElementKind.Text => Contracts.Faces.FaceElementKind.Text,
                Core.Boards.FaceElementKind.Clock => Contracts.Faces.FaceElementKind.Clock,
                _ => throw new ArgumentOutOfRangeException(nameof(element), element.Kind, null)
            },
            Anchor = element.Anchor switch
            {
                Core.Boards.FaceAnchor.Face => Contracts.Faces.FaceAnchor.Face,
                Core.Boards.FaceAnchor.Matrix => Contracts.Faces.FaceAnchor.Matrix,
                Core.Boards.FaceAnchor.Row => Contracts.Faces.FaceAnchor.Row,
                Core.Boards.FaceAnchor.Top => Contracts.Faces.FaceAnchor.Top,
                Core.Boards.FaceAnchor.Bottom => Contracts.Faces.FaceAnchor.Bottom,
                Core.Boards.FaceAnchor.Left => Contracts.Faces.FaceAnchor.Left,
                Core.Boards.FaceAnchor.Right => Contracts.Faces.FaceAnchor.Right,
                _ => throw new ArgumentOutOfRangeException(nameof(element), element.Anchor, null)
            },
            Row = element.Row,
            X = element.X,
            Y = element.Y,
            Width = element.Width,
            Height = element.Height,
            Align = element.Align switch
            {
                Core.Boards.FaceAlign.Left => Contracts.Faces.FaceAlign.Left,
                Core.Boards.FaceAlign.Center => Contracts.Faces.FaceAlign.Center,
                Core.Boards.FaceAlign.Right => Contracts.Faces.FaceAlign.Right,
                _ => throw new ArgumentOutOfRangeException(nameof(element), element.Align, null)
            },
            VAlign = element.VAlign switch
            {
                Core.Boards.FaceVAlign.Top => Contracts.Faces.FaceVAlign.Top,
                Core.Boards.FaceVAlign.Middle => Contracts.Faces.FaceVAlign.Middle,
                Core.Boards.FaceVAlign.Bottom => Contracts.Faces.FaceVAlign.Bottom,
                _ => throw new ArgumentOutOfRangeException(nameof(element), element.VAlign, null)
            },
            Text = element.Text,
            Size = element.Size,
            Size2 = element.Size2,
            Bold = element.Bold,
            Color = element.Color,
            Fill = element.Fill,
            Border = element.Border,
            Radius = element.Radius,
            MaxWidth = element.MaxWidth,
            Spacing = element.Spacing,
            Format = element.Format switch
            {
                Core.Boards.FaceClockFormat.HourMinute => Contracts.Faces.FaceClockFormat.HourMinute,
                Core.Boards.FaceClockFormat.HourMinutePadded => Contracts.Faces.FaceClockFormat.HourMinutePadded,
                Core.Boards.FaceClockFormat.WithSeconds => Contracts.Faces.FaceClockFormat.WithSeconds,
                _ => throw new ArgumentOutOfRangeException(nameof(element), element.Format, null)
            }
        };
    }
}
