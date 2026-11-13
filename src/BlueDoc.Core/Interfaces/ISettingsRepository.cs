using System.Threading.Tasks;

namespace BlueDoc.Core.Interfaces
{
    public interface ISettingsRepository
    {
        Task<string> GetSettingAsync(string key, string defaultValue = "");
        Task SetSettingAsync(string key, string value);
        Task<bool> GetBoolSettingAsync(string key, bool defaultValue = false);
        Task SetBoolSettingAsync(string key, bool value);
    }
}
