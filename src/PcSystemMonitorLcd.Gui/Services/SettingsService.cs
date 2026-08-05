using System;
using System.IO;
using System.Text.Json;

namespace PcSystemMonitorLcd.Gui.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly string _filePath;

        public SettingsService()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PC Monitor");
            Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, "settings.json");
        }

        public AppSettings Load()
        {
            try
            {
                if (!File.Exists(_filePath))
                    return new AppSettings();

                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                // Corrupt or unreadable settings file — fall back to defaults rather than crash
                return new AppSettings();
            }
        }

        public void Save(AppSettings settings)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
            catch
            {
                // Best-effort — don't crash the app if the write fails (e.g. locked file, no perms)
            }
        }
    }
}
