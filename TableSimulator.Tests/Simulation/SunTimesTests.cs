using TableSimulator.Core.Boards;

namespace TableSimulator.Tests.Simulation;

/// <summary>
/// Vychod a zapad slnka pre snimac tmy ustredne AK Signal - porovnanie s tabulkovymi hodnotami pre Bratislavu
/// (tolerancia 3 minuty), polarny den a noc a posun snimaca.
/// </summary>
[TestClass]
public class SunTimesTests
{
    private static readonly GeoLocation Bratislava = new(48.1486, 17.1077);
    private static readonly GeoLocation Svalbard = new(78.22, 15.65);

    private static void Near(DateTimeOffset expected, DateTimeOffset? actual)
    {
        Assert.IsNotNull(actual);
        Assert.IsLessThan(3, Math.Abs((expected - actual.Value).TotalMinutes), $"{expected:u} != {actual:u}");
    }

    [TestMethod]
    public void VychodZapad_Bratislava()
    {
        // leto: vychod 4:51, zapad 20:56 SELC; zima: 7:39 a 15:59 SEC
        var (rise, set) = SunTimes.For(new DateOnly(2025, 6, 21), Bratislava);
        Near(new DateTimeOffset(2025, 6, 21, 2, 51, 0, TimeSpan.Zero), rise);
        Near(new DateTimeOffset(2025, 6, 21, 18, 56, 0, TimeSpan.Zero), set);

        (rise, set) = SunTimes.For(new DateOnly(2025, 12, 21), Bratislava);
        Near(new DateTimeOffset(2025, 12, 21, 6, 39, 0, TimeSpan.Zero), rise);
        Near(new DateTimeOffset(2025, 12, 21, 14, 59, 0, TimeSpan.Zero), set);
    }

    [TestMethod]
    public void Tma_PodlaVychoduAZapadu()
    {
        Assert.IsFalse(SunTimes.IsDark(new DateTimeOffset(2025, 12, 21, 12, 0, 0, TimeSpan.Zero), Bratislava));
        Assert.IsTrue(SunTimes.IsDark(new DateTimeOffset(2025, 12, 21, 15, 30, 0, TimeSpan.Zero), Bratislava));
        Assert.IsTrue(SunTimes.IsDark(new DateTimeOffset(2025, 12, 21, 2, 0, 0, TimeSpan.Zero), Bratislava));
        Assert.IsTrue(SunTimes.IsDark(new DateTimeOffset(2025, 12, 21, 23, 59, 0, TimeSpan.Zero), Bratislava));
    }

    [TestMethod]
    public void Tma_PosunSkracujeAPredlzuje()
    {
        // 20 minut po zapade (okolo 14:59 UTC)
        var after = new DateTimeOffset(2025, 12, 21, 15, 20, 0, TimeSpan.Zero);
        Assert.IsTrue(SunTimes.IsDark(after, Bratislava));
        Assert.IsFalse(SunTimes.IsDark(after, Bratislava, 30), "tma zacne az 30 minut po zapade");

        // 20 minut pred zapadom
        var before = new DateTimeOffset(2025, 12, 21, 14, 40, 0, TimeSpan.Zero);
        Assert.IsFalse(SunTimes.IsDark(before, Bratislava));
        Assert.IsTrue(SunTimes.IsDark(before, Bratislava, -30), "zaporny posun tmu predlzi");
    }

    [TestMethod]
    public void PolarnyDenANoc()
    {
        Assert.AreEqual((null, null), SunTimes.For(new DateOnly(2025, 6, 21), Svalbard));
        Assert.IsTrue(SunTimes.IsPolarDay(new DateOnly(2025, 6, 21), Svalbard));
        Assert.IsFalse(SunTimes.IsDark(new DateTimeOffset(2025, 6, 21, 0, 0, 0, TimeSpan.Zero), Svalbard));
        Assert.IsTrue(SunTimes.IsDark(new DateTimeOffset(2025, 12, 21, 12, 0, 0, TimeSpan.Zero), Svalbard));
    }
}
