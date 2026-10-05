using Microsoft.Extensions.FileProviders;
using TableSimulator.Application.Settings;

namespace TableSimulator.Infrastructure.Hosting;

/// <summary>Staticke subory servera okrem weboveho rozhrania.</summary>
public static class StaticFilesExtensions
{
    /// <summary>Vlastne vzhlady pod <c>/designs</c> - maju prednost pred vstavanymi s rovnakym id.</summary>
    public static WebApplication UseDesignFiles(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var paths = app.Services.GetRequiredService<AppPaths>();
        var folder = paths.DesignsFolder(app.Services.GetRequiredService<ISettingsStore>().Current);
        if (Directory.Exists(folder))
            app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(folder), RequestPath = "/designs" });
        return app;
    }
}
