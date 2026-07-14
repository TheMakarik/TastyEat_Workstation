namespace TastyEat.Workstation.Services.Interfaces;

public interface IApplicationSettingsService
{
    Task<bool> GetBoolAsync(string key, bool defaultValue = false, CancellationToken cancellationToken = default);
    Task SetBoolAsync(string key, bool value, CancellationToken cancellationToken = default);
}
