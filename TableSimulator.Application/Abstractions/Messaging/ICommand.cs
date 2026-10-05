namespace TableSimulator.Application.Abstractions.Messaging;

/// <summary>Prikaz bez vysledku - meni stav simulatora alebo nastavenia.</summary>
public interface ICommand;

/// <summary>Prikaz s vysledkom <typeparamref name="TResponse" />.</summary>
public interface ICommand<TResponse>;
