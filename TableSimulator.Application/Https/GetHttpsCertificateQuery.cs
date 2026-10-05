using TableSimulator.Application.Abstractions.Messaging;

namespace TableSimulator.Application.Https;

/// <summary>Certifikat, ktoremu maju zariadenia doverovat (na nainstalovanie).</summary>
public sealed record GetHttpsCertificateQuery : IQuery<HttpsCertificateFile>;
