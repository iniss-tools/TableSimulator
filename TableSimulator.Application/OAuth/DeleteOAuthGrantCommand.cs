using System.Globalization;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Audit;

namespace TableSimulator.Application.OAuth;

/// <summary>Zrusi povolenie klienta MCP - jeho tokeny prestanu platit hned.</summary>
public sealed record DeleteOAuthGrantCommand(Guid Id) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => Id.ToString("D", CultureInfo.InvariantCulture);
}
