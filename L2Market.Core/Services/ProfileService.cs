using System;
using System.IO;
using System.Text;
using L2Market.Domain.Models;
using Microsoft.Extensions.Logging;

namespace L2Market.Core.Services
{
    /// <summary>
    /// Сервис для работы с профилями игроков
    /// </summary>
    public class ProfileService
    {
        private readonly ILogger<ProfileService> _logger;
        private readonly string _profilesDirectory;

        public ProfileService(ILogger<ProfileService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _profilesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "profiles");
            
            // Создаем папку profiles если её нет
            if (!Directory.Exists(_profilesDirectory))
            {
                Directory.CreateDirectory(_profilesDirectory);
                _logger.LogInformation("Created profiles directory: {ProfilesDirectory}", _profilesDirectory);
            }
        }

        /// <summary>
        /// Сохраняет профиль игрока в INI файл
        /// </summary>
        public bool SaveProfile(PlayerProfile profile)
        {
            try
            {
                if (string.IsNullOrEmpty(profile.PlayerName))
                {
                    _logger.LogWarning("Cannot save profile: Player name is empty");
                    return false;
                }

                var fileName = SanitizeFileName(profile.PlayerName) + ".ini";
                var filePath = Path.Combine(_profilesDirectory, fileName);

                var iniContent = new StringBuilder();
                iniContent.AppendLine("[Profile]");
                iniContent.AppendLine($"PlayerName={profile.PlayerName}");
                iniContent.AppendLine($"Server={profile.Server}");
                iniContent.AppendLine($"PrivateStoreTracking={profile.IsPrivateStoreTrackingEnabled}");
                iniContent.AppendLine($"CommissionTracking={profile.IsCommissionTrackingEnabled}");
                iniContent.AppendLine($"WorldExchangeTracking={profile.IsWorldExchangeTrackingEnabled}");
                iniContent.AppendLine($"AutoStartTracking={profile.AutoStartTracking}");

                File.WriteAllText(filePath, iniContent.ToString(), Encoding.UTF8);
                _logger.LogInformation("Profile saved: {PlayerName} -> {FilePath}", profile.PlayerName, filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving profile for player: {PlayerName}", profile.PlayerName);
                return false;
            }
        }

        /// <summary>
        /// Загружает профиль игрока из INI файла
        /// </summary>
        public PlayerProfile? LoadProfile(string playerName)
        {
            try
            {
                if (string.IsNullOrEmpty(playerName))
                {
                    _logger.LogWarning("Cannot load profile: Player name is empty");
                    return null;
                }

                var fileName = SanitizeFileName(playerName) + ".ini";
                var filePath = Path.Combine(_profilesDirectory, fileName);

                if (!File.Exists(filePath))
                {
                    _logger.LogDebug("Profile file not found: {FilePath}", filePath);
                    return null;
                }

                var profile = new PlayerProfile();
                var lines = File.ReadAllLines(filePath, Encoding.UTF8);

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("["))
                        continue;

                    var parts = line.Split('=', 2);
                    if (parts.Length != 2)
                        continue;

                    var key = parts[0].Trim();
                    var value = parts[1].Trim();

                    switch (key)
                    {
                        case "PlayerName":
                            profile.PlayerName = value;
                            break;
                        case "Server":
                            profile.Server = value;
                            break;
                        case "PrivateStoreTracking":
                            profile.IsPrivateStoreTrackingEnabled = bool.Parse(value);
                            break;
                        case "CommissionTracking":
                            profile.IsCommissionTrackingEnabled = bool.Parse(value);
                            break;
                        case "WorldExchangeTracking":
                            profile.IsWorldExchangeTrackingEnabled = bool.Parse(value);
                            break;
                        case "AutoStartTracking":
                            profile.AutoStartTracking = bool.Parse(value);
                            break;
                    }
                }

                _logger.LogInformation("Profile loaded: {PlayerName} from {FilePath}", profile.PlayerName, filePath);
                return profile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile for player: {PlayerName}", playerName);
                return null;
            }
        }

        /// <summary>
        /// Проверяет, существует ли профиль для игрока
        /// </summary>
        public bool ProfileExists(string playerName)
        {
            try
            {
                if (string.IsNullOrEmpty(playerName))
                    return false;

                var fileName = SanitizeFileName(playerName) + ".ini";
                var filePath = Path.Combine(_profilesDirectory, fileName);
                return File.Exists(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if profile exists for player: {PlayerName}", playerName);
                return false;
            }
        }

        /// <summary>
        /// Удаляет профиль игрока
        /// </summary>
        public bool DeleteProfile(string playerName)
        {
            try
            {
                if (string.IsNullOrEmpty(playerName))
                    return false;

                var fileName = SanitizeFileName(playerName) + ".ini";
                var filePath = Path.Combine(_profilesDirectory, fileName);

                if (!File.Exists(filePath))
                    return false;

                File.Delete(filePath);
                _logger.LogInformation("Profile deleted: {PlayerName} from {FilePath}", playerName, filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting profile for player: {PlayerName}", playerName);
                return false;
            }
        }

        /// <summary>
        /// Очищает имя файла от недопустимых символов
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName;
        }
    }
}
