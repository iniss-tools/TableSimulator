using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Archive;
using TableSimulator.Application.Audit;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Decorators;
using TableSimulator.Application.Designs;
using TableSimulator.Application.Fonts;
using TableSimulator.Application.Recordings;
using TableSimulator.Application.Scenarios;
using TableSimulator.Application.Signage;
using TableSimulator.Application.Station;
using TableSimulator.Application.Virtual;
using TableSimulator.Core.Virtual;

namespace TableSimulator.Application;

/// <summary>Registracia aplikacnej vrstvy: handlery prikazov a dotazov, dekoratory, validatory a sluzby.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Handlery najde v tomto zostaveni (Scrutor). Dekoratory obalia kazdy handler - posledny pridany je vonkajsi:
    /// zaznam (cas, chyba) obsahuje audit a ten overenie vstupov - do auditu ide aj prikaz s neplatnymi vstupmi.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.Scan(scan => scan.FromAssembliesOf(typeof(DependencyInjection))
            .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)).Where(t => !t.IsGenericTypeDefinition), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<>)).Where(t => !t.IsGenericTypeDefinition), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<,>)).Where(t => !t.IsGenericTypeDefinition), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.Decorate(typeof(IQueryHandler<,>), typeof(ValidationQueryHandler<,>));
        services.Decorate(typeof(ICommandHandler<,>), typeof(ValidationCommandHandler<,>));
        services.Decorate(typeof(ICommandHandler<>), typeof(ValidationCommandHandler<>));

        services.Decorate(typeof(ICommandHandler<,>), typeof(AuditCommandHandler<,>));
        services.Decorate(typeof(ICommandHandler<>), typeof(AuditCommandHandler<>));

        services.Decorate(typeof(IQueryHandler<,>), typeof(LoggingQueryHandler<,>));
        services.Decorate(typeof(ICommandHandler<,>), typeof(LoggingCommandHandler<,>));
        services.Decorate(typeof(ICommandHandler<>), typeof(LoggingCommandHandler<>));

        // popisy chyb su anglicke a nezavisle od jazyka systemu - text pre obsluhu sklada webove rozhranie podla kodu chyby
        ValidatorOptions.Global.LanguageManager.Enabled = false;
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        services.AddSingleton<RoleCatalog>();
        services.AddSingleton<RecordingService>();
        services.AddSingleton<FontCatalog>();
        services.AddSingleton<DesignCatalog>();
        services.AddScoped<AuditWriter>();
        services.AddSingleton<ArchiveCatalog>();
        services.AddSingleton<ReportMonitor>();
        services.AddScoped<StationImporter>();
        services.AddSingleton<SignagePanelTracker>();
        services.AddSingleton<ScenarioRunner>();
        services.AddSingleton<SignageMediaStore>();
        services.AddSingleton<XslTablePages>();
        services.AddSingleton<InissViewSite>();
        return services;
    }
}
