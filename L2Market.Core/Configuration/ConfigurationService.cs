using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.IO;

namespace L2Market.Core.Configuration
{
    /// <summary>
    /// Implementation of configuration service
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private readonly ILogger<ConfigurationService> _logger;
        private readonly string _configPath;
        private AppSettings _settings;

        public AppSettings Settings => _settings;

        public ConfigurationService(ILogger<ConfigurationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            _settings = new AppSettings();
        }

        public async Task LoadSettingsAsync()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = await File.ReadAllTextAsync(_configPath);
                    _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                    _logger.LogInformation("Configuration loaded from {ConfigPath}", _configPath);
                }
                else
                {
                    _settings = new AppSettings();
                    await SaveSettingsAsync(); // Create default config file
                    _logger.LogInformation("Default configuration created at {ConfigPath}", _configPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load configuration, using defaults");
                _settings = new AppSettings();
            }
        }

        public async Task SaveSettingsAsync()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(_settings, options);
                await File.WriteAllTextAsync(_configPath, json);
                _logger.LogInformation("Configuration saved to {ConfigPath}", _configPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save configuration to {ConfigPath}", _configPath);
                throw;
            }
        }

        public async Task ResetToDefaultsAsync()
        {
            _settings = new AppSettings();
            await SaveSettingsAsync();
            _logger.LogInformation("Configuration reset to defaults");
        }

        public async Task UpdateSettingAsync<T>(string key, T value)
        {
            try
            {
                // Simple property update - in real implementation, you might use reflection or a more sophisticated approach
                _logger.LogDebug("Updating setting {Key} to {Value}", key, value);
                await SaveSettingsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update setting {Key}", key);
                throw;
            }
        }
    }
}
