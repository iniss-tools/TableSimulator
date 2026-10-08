namespace TableSimulator.Contracts.Signage;

/// <summary>Sprava informacneho panelu (POST /api/signage/messages, PUT /api/signage/messages/{id}); pouzije sa len cast druhu.</summary>
/// <param name="Name">nazov pre obsluhu</param>
/// <param name="Note">poznamka</param>
/// <param name="Kind">druh</param>
/// <param name="Duration">trvanie v playliste v sekundach (polozka playlistu ho moze zmenit)</param>
/// <param name="VirtualBoard">virtualna tabula (<see cref="SignageMessageKind.Virtual" />)</param>
/// <param name="Board">tabula simulatora <c>linka-adresa</c> (<see cref="SignageMessageKind.Board" />)</param>
/// <param name="Paragraphs">odseky (<see cref="SignageMessageKind.Text" />)</param>
/// <param name="Background">farba pozadia <c>#rrggbb</c> (text, obrazok, video)</param>
/// <param name="Media">nahraty subor (<see cref="SignageMessageKind.Image" />, <see cref="SignageMessageKind.Video" />)</param>
/// <param name="Url">adresa stranky (<see cref="SignageMessageKind.Web" />)</param>
public sealed record SetSignageMessageRequest(
    string Name,
    string Note,
    SignageMessageKind Kind,
    int Duration,
    int? VirtualBoard,
    string? Board,
    IReadOnlyList<SignageParagraphDto> Paragraphs,
    string Background,
    Guid? Media,
    string? Url);
