namespace TableSimulator.Contracts.AkSignal;

/// <summary>Stav svetla ustredne AK Signal od casu <paramref name="At" /> do dalsej zmeny.</summary>
/// <param name="BoardId">ustredna <c>linka-adresa</c></param>
/// <param name="At">cas zmeny</param>
/// <param name="Auto">automaticke svietenie</param>
/// <param name="Manual">rucne svietenie</param>
/// <param name="Dark">snimac hlasi tmu</param>
/// <param name="On">svetla svietia</param>
/// <param name="Fault">porucha svetiel</param>
/// <param name="Silent">server sa ustredne nepytal</param>
/// <param name="PoweredOff">ustredna bez napajania (scenar)</param>
public sealed record LightStateDto(string BoardId, DateTimeOffset At, bool Auto, bool Manual, bool Dark, bool On, bool Fault, bool Silent, bool PoweredOff);
