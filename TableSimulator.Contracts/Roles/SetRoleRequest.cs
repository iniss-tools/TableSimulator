namespace TableSimulator.Contracts.Roles;

/// <summary>Nova alebo upravena rola; pri vstavanej sa nazov nemeni.</summary>
/// <param name="DisplayName">nazov roly</param>
/// <param name="Permissions">opravnenia - vzdy aj <c>simulator.view</c></param>
public sealed record SetRoleRequest(string DisplayName, IReadOnlyList<string> Permissions);
