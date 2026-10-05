using JetBrains.Annotations;
using Microsoft.AspNetCore.Routing;

namespace TableSimulator.Api;

/// <summary>Jeden endpoint API - mapuje sa do skupiny <c>/api</c>.</summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
