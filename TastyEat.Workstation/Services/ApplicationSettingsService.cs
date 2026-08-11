using Microsoft.EntityFrameworkCore;
using TastyEat.Workstation.Models;
using TastyEat.Workstation.Services.Interfaces;

namespace TastyEat.Workstation.Services;

public sealed class ApplicationSettingsService(DataContext context) : IApplicationSettingsService
{
    public async Task<bool> GetBoolAsync(string key, bool defaultValue = false, CancellationToken cancellationToken = default)
    {
        var setting = await context.ApplicationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

        if (setting is null)
            return defaultValue;

        return bool.TryParse(setting.Value, out var value) ? value : defaultValue;
    }

    public async Task SetBoolAsync(string key, bool value, CancellationToken cancellationToken = default)
    {
        var setting = await context.ApplicationSettings
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

        if (setting is null)
        {
            setting = new Models.Tables.ApplicationSetting { Key = key, Value = value.ToString() };
            context.ApplicationSettings.Add(setting);
        }
        else
        {
            setting.Value = value.ToString();
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
