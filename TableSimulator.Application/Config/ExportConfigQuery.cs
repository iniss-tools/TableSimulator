using TableSimulator.Application.Abstractions.Messaging;

namespace TableSimulator.Application.Config;

/// <summary>Linky a tabule ako subor JSON (rovnaky tvar ako povodny <c>config.json</c>).</summary>
public sealed record ExportConfigQuery : IQuery<ConfigFile>;
