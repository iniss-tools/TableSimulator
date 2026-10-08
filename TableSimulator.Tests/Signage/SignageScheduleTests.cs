using TableSimulator.Application.Signage;
using TableSimulator.Contracts.Signage;

namespace TableSimulator.Tests.Signage;

/// <summary>
/// Rozvrh playlistov informacnych panelov ako v INISSView 2: platnost podla dna, casove okno (aj cez polnoc), priorita
/// druhov, predvoleny playlist a sprava, najblizsia zmena a striedanie poloziek.
/// </summary>
[TestClass]
public class SignageScheduleTests
{
    // 2026-10-08 je stvrtok
    private static readonly DateOnly Day = new(2026, 10, 8);

    private static DateTime At(int hour, int minute = 0, int day = 0) => Day.AddDays(day).ToDateTime(new TimeOnly(hour, minute));

    private static SignagePlaylistDto Playlist(int id, SignagePeriod period, int from = 0, int to = 0, DateOnly? start = null, DateOnly? end = null,
        params int[] weekdays) =>
        new(id, new SetSignagePlaylistRequest($"P{id}", period, start ?? Day, end, weekdays, from, to, [new SignagePlaylistItemDto(1, null)]));

    [TestMethod]
    public void JedenDen_PlatiLenVTenDenAVOkne()
    {
        var once = Playlist(1, SignagePeriod.Once, 8 * 60, 12 * 60);

        Assert.AreEqual(1, SignageSchedule.Select([once], null, null, At(9)).Playlist?.Id);
        Assert.IsNull(SignageSchedule.Select([once], null, null, At(12)).Playlist);
        Assert.IsNull(SignageSchedule.Select([once], null, null, At(9, day: 1)).Playlist);
        Assert.AreEqual(At(8), SignageSchedule.Select([once], null, null, At(9)).Anchor);
    }

    [TestMethod]
    public void Priorita_JedenDenPredDnamiVTyzdniAKazdymDnom()
    {
        var daily = Playlist(1, SignagePeriod.Daily);
        var weekly = Playlist(2, SignagePeriod.Weekly, weekdays: 4);
        var once = Playlist(3, SignagePeriod.Once, 10 * 60, 11 * 60);

        Assert.AreEqual(2, SignageSchedule.Select([daily, weekly, once], null, null, At(9)).Playlist?.Id);
        Assert.AreEqual(3, SignageSchedule.Select([daily, weekly, once], null, null, At(10, 30)).Playlist?.Id);
        // piatok - dni v tyzdni neplatia
        Assert.AreEqual(1, SignageSchedule.Select([daily, weekly, once], null, null, At(9, day: 1)).Playlist?.Id);
    }

    [TestMethod]
    public void OknoCezPolnoc_PatriDnuZaciatku()
    {
        var night = Playlist(1, SignagePeriod.Once, 22 * 60, 2 * 60);

        Assert.AreEqual(At(22), SignageSchedule.Select([night], null, null, At(23)).Anchor);
        Assert.AreEqual(At(22), SignageSchedule.Select([night], null, null, At(1, day: 1)).Anchor);
        Assert.IsNull(SignageSchedule.Select([night], null, null, At(1)).Playlist);
    }

    [TestMethod]
    public void RovnakyZaciatokAKoniec_CelyDen()
    {
        var all = Playlist(1, SignagePeriod.Daily, 6 * 60, 6 * 60, end: Day.AddDays(1));

        Assert.AreEqual(At(6), SignageSchedule.Select([all], null, null, At(23)).Anchor);
        Assert.AreEqual(At(6), SignageSchedule.Select([all], null, null, At(5, day: 1)).Anchor);
        Assert.IsNull(SignageSchedule.Select([all], null, null, At(5)).Playlist);
    }

    [TestMethod]
    public void Predvoleny_OdKoncaPoslednehoOknaInakPredvolenaSprava()
    {
        var morning = Playlist(1, SignagePeriod.Daily, 6 * 60, 9 * 60);
        var fallback = Playlist(9, SignagePeriod.Daily);

        var before = SignageSchedule.Select([morning], fallback, null, At(5));
        Assert.AreEqual(9, before.Playlist?.Id);
        Assert.AreEqual(At(0), before.Anchor);
        Assert.AreEqual(At(9), SignageSchedule.Select([morning], fallback, null, At(10)).Anchor);

        var message = SignageSchedule.Select([morning], null, 7, At(10));
        Assert.IsNull(message.Playlist);
        Assert.AreEqual(7, message.Message);
        Assert.IsNull(SignageSchedule.Select([morning], null, null, At(10)).Message);
    }

    [TestMethod]
    public void NajblizsiaZmena_ZaciatokAKoniecOkna()
    {
        var once = Playlist(1, SignagePeriod.Once, 8 * 60, 12 * 60);

        Assert.AreEqual(At(8), SignageSchedule.NextChange([once], null, null, At(7, 15)));
        Assert.AreEqual(At(12), SignageSchedule.NextChange([once], null, null, At(9)));
        // nic sa nezmeni - najneskor o den
        Assert.AreEqual(At(13, day: 1), SignageSchedule.NextChange([once], null, null, At(13)));
    }

    [TestMethod]
    public void Striedanie_PodlaTrvaniOdKotvy()
    {
        int[] durations = [10, 20, 30];

        Assert.AreEqual(0, SignageSchedule.SlotAt(durations, At(8), At(8)));
        Assert.AreEqual(1, SignageSchedule.SlotAt(durations, At(8), At(8).AddSeconds(10)));
        Assert.AreEqual(2, SignageSchedule.SlotAt(durations, At(8), At(8).AddSeconds(59)));
        Assert.AreEqual(0, SignageSchedule.SlotAt(durations, At(8), At(8).AddSeconds(60)));
        Assert.AreEqual(1, SignageSchedule.SlotAt(durations, At(8), At(9).AddSeconds(15)));
    }
}
