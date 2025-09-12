using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace L2Market.Core.Configuration
{
    /// <summary>
    /// INI-based configuration service implementation
    /// </summary>
    public class IniConfigurationService : IConfigurationService
    {
        private readonly ILogger<IniConfigurationService> _logger;
        private readonly string _configPath;
        private AppSettings _settings;

        public AppSettings Settings => _settings;

        public IniConfigurationService(ILogger<IniConfigurationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "L2Market.ini");
            _settings = new AppSettings();
        }

        public async Task LoadSettingsAsync()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var iniContent = await File.ReadAllTextAsync(_configPath);
                    _settings = ParseIniFile(iniContent);
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
                var iniContent = CreateIniContent(_settings);
                await File.WriteAllTextAsync(_configPath, iniContent);
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
                _logger.LogDebug("Updating setting {Key} to {Value}", key, value);
                await SaveSettingsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update setting {Key}", key);
                throw;
            }
        }

        private AppSettings ParseIniFile(string iniContent)
        {
            var settings = new AppSettings();
            var lines = iniContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var currentSection = "";

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";"))
                    continue;

                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                    continue;
                }

                var equalIndex = trimmedLine.IndexOf('=');
                if (equalIndex <= 0) continue;

                var key = trimmedLine.Substring(0, equalIndex).Trim();
                var value = trimmedLine.Substring(equalIndex + 1).Trim();

                switch (currentSection.ToLower())
                {
                    case "namedpipe":
                        ParseNamedPipeSetting(settings.NamedPipe, key, value);
                        break;
                    case "injection":
                        ParseInjectionSetting(settings.Injection, key, value);
                        break;
                    case "ui":
                        ParseUISetting(settings.UI, key, value);
                        break;
                }
            }

            return settings;
        }

        private void ParseNamedPipeSetting(NamedPipeSettings settings, string key, string value)
        {
            switch (key.ToLower())
            {
                case "connectiontimeout":
                    if (double.TryParse(value, out double connectionTimeout))
                        settings.ConnectionTimeout = TimeSpan.FromSeconds(connectionTimeout);
                    break;
                case "retrydelay":
                    if (double.TryParse(value, out double retryDelay))
                        settings.RetryDelay = TimeSpan.FromSeconds(retryDelay);
                    break;
                case "maxretries":
                    if (int.TryParse(value, out int maxRetries))
                        settings.MaxRetries = maxRetries;
                    break;
                case "readtimeout":
                    if (double.TryParse(value, out double readTimeout))
                        settings.ReadTimeout = TimeSpan.FromSeconds(readTimeout);
                    break;
                case "servershutdowntimeout":
                    if (double.TryParse(value, out double serverShutdownTimeout))
                        settings.ServerShutdownTimeout = TimeSpan.FromSeconds(serverShutdownTimeout);
                    break;
            }
        }

        private void ParseInjectionSetting(InjectionSettings settings, string key, string value)
        {
            switch (key.ToLower())
            {
                case "workflowtimeout":
                    if (double.TryParse(value, out double workflowTimeout))
                        settings.WorkflowTimeout = TimeSpan.FromSeconds(workflowTimeout);
                    break;
                case "processsearchtimeout":
                    if (double.TryParse(value, out double processSearchTimeout))
                        settings.ProcessSearchTimeout = TimeSpan.FromSeconds(processSearchTimeout);
                    break;
                case "defaultprocessname":
                    settings.DefaultProcessName = value;
                    break;
                case "defaultdllpath":
                    settings.DefaultDllPath = value;
                    break;
            }
        }

        private void ParseUISetting(UISettings settings, string key, string value)
        {
            switch (key.ToLower())
            {
                case "autoscroll":
                    if (bool.TryParse(value, out bool autoScroll))
                        settings.AutoScroll = autoScroll;
                    break;
                case "maxloglines":
                    if (int.TryParse(value, out int maxLogLines))
                        settings.MaxLogLines = maxLogLines;
                    break;
                case "showtimestamps":
                    if (bool.TryParse(value, out bool showTimestamps))
                        settings.ShowTimestamps = showTimestamps;
                    break;
                case "theme":
                    settings.Theme = value;
                    break;
                case "minimizetotray":
                    if (bool.TryParse(value, out bool minimizeToTray))
                        settings.MinimizeToTray = minimizeToTray;
                    break;
                case "autostartenabled":
                    if (bool.TryParse(value, out bool autoStartEnabled))
                        settings.AutoStartEnabled = autoStartEnabled;
                    break;
            }
        }

        private string CreateIniContent(AppSettings settings)
        {
            var lines = new List<string>
            {
                "; L2Market Configuration File",
                "; Generated automatically - do not edit manually unless you know what you're doing",
                "",
                "[NamedPipe]",
                $"ConnectionTimeout={settings.NamedPipe.ConnectionTimeout.TotalSeconds}",
                $"RetryDelay={settings.NamedPipe.RetryDelay.TotalSeconds}",
                $"MaxRetries={settings.NamedPipe.MaxRetries}",
                $"ReadTimeout={settings.NamedPipe.ReadTimeout.TotalSeconds}",
                $"ServerShutdownTimeout={settings.NamedPipe.ServerShutdownTimeout.TotalSeconds}",
                "",
                "[Injection]",
                $"WorkflowTimeout={settings.Injection.WorkflowTimeout.TotalSeconds}",
                $"ProcessSearchTimeout={settings.Injection.ProcessSearchTimeout.TotalSeconds}",
                $"DefaultProcessName={settings.Injection.DefaultProcessName}",
                $"DefaultDllPath={settings.Injection.DefaultDllPath}",
                "",
                "[UI]",
                $"AutoScroll={settings.UI.AutoScroll}",
                $"MaxLogLines={settings.UI.MaxLogLines}",
                $"ShowTimestamps={settings.UI.ShowTimestamps}",
                $"Theme={settings.UI.Theme}",
                $"MinimizeToTray={settings.UI.MinimizeToTray}",
                $"AutoStartEnabled={settings.UI.AutoStartEnabled}",
                ""
            };

            return string.Join("\n", lines);
        }
    }
}
