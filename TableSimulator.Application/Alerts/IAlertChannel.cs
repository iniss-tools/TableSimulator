namespace TableSimulator.Application.Alerts;

/// <summary>Kanal oznameni (webhook, e-mail) - implementuje server.</summary>
public interface IAlertChannel
{
    /// <summary>Kluc kanala: <c>webhook</c> alebo <c>email</c>.</summary>
    string Key { get; }

    /// <summary>Kanal je v nastaveni vyplneny.</summary>
    bool IsConfigured(AlertSettings settings);

    /// <summary>Posle oznamenie.</summary>
    /// <exception cref="Exception">kanal oznamenie neprijal (sprava sa zapise do logu alebo vrati pri skuske)</exception>
    Task SendAsync(AlertMessage message, AlertSettings settings, CancellationToken cancellationToken);
}
