using FluentValidation;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;
using TableSimulator.Application.Settings;
using TableSimulator.Contracts.Settings;

namespace TableSimulator.Application.Alerts;

/// <summary>Posle skusobne oznamenie podla nastavenia z formulara (heslo SMTP null = ulozene).</summary>
public sealed record TestAlertsCommand(AlertSettingsRequest Request) : ICommand<AlertTestResultDto>, IAuditable
{
    string IAuditable.AuditTarget => "alerts";
}

public sealed class TestAlertsCommandValidator : AbstractValidator<TestAlertsCommand>
{
    public TestAlertsCommandValidator() => RuleFor(c => c.Request).NotNull().WithErrorCode(AlertErrors.Webhook).SetValidator(new AlertSettingsRules());
}

public sealed class TestAlertsCommandHandler(
    ISettingsStore settings,
    ISecretProtector secrets,
    IEnumerable<IAlertChannel> channels,
    TimeProvider time) : ICommandHandler<TestAlertsCommand, AlertTestResultDto>
{
    public async Task<Result<AlertTestResultDto>> Handle(TestAlertsCommand command, CancellationToken cancellationToken)
    {
        var alerts = AlertSettingsMapper.Apply(settings.Current.Alerts, command.Request, secrets);
        var message = AlertMessage.Create(AlertKind.Test, [], time.GetUtcNow(), Environment.MachineName, alerts.Language, time.LocalTimeZone);
        var results = new Dictionary<string, AlertChannelResultDto>(StringComparer.Ordinal);
        foreach (var channel in channels)
            results[channel.Key] = await Send(channel, message, alerts, cancellationToken).ConfigureAwait(false);
        var none = new AlertChannelResultDto(false, null);
        return Result.Success(new AlertTestResultDto(results.GetValueOrDefault("webhook", none), results.GetValueOrDefault("email", none)));
    }

    private static async Task<AlertChannelResultDto> Send(IAlertChannel channel, AlertMessage message, AlertSettings alerts, CancellationToken cancellationToken)
    {
        if (!channel.IsConfigured(alerts))
            return new AlertChannelResultDto(false, null);
        try
        {
            await channel.SendAsync(message, alerts, cancellationToken).ConfigureAwait(false);
            return new AlertChannelResultDto(true, null);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            return new AlertChannelResultDto(true, e.Message);
        }
    }
}
