using L2Market.Core.Configuration;

namespace L2Market.Core.Configuration
{
    /// <summary>
    /// Service for managing application configuration
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// Gets the current application settings
        /// </summary>
        AppSettings Settings { get; }

        /// <summary>
        /// Loads settings from configuration file
        /// </summary>
        Task LoadSettingsAsync();

        /// <summary>
        /// Saves settings to configuration file
        /// </summary>
        Task SaveSettingsAsync();

        /// <summary>
        /// Resets settings to default values
        /// </summary>
        Task ResetToDefaultsAsync();

        /// <summary>
        /// Updates a specific setting
        /// </summary>
        Task UpdateSettingAsync<T>(string key, T value);
    }
}
