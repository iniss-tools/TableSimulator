namespace TableSimulator.Contracts.Settings;

/// <summary>Vysledok skusobneho oznamenia (POST /api/settings/alerts/test).</summary>
public sealed record AlertTestResultDto(AlertChannelResultDto Webhook, AlertChannelResultDto Email);
