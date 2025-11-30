#nullable enable
using Code_Crammer.Data.Classes.Models;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Code_Crammer.Data.Classes.Services
{
    public static class ProfileManager
    {
        private const int MAX_HISTORY_FILES = 10;

        public static Action<string>? OnError;

        public static ProfileData? LoadProfile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                OnError?.Invoke($"Profile not found: {filePath}");
                return null;
            }

            try
            {
                string jsonString = File.ReadAllText(filePath);
                var data = JsonConvert.DeserializeObject<ProfileData>(jsonString);
                if (data == null)
                {
                    OnError?.Invoke($"Profile is empty or corrupt: {filePath}");
                }
                return data;
            }
            catch (JsonException jex)
            {
                OnError?.Invoke($"JSON Error loading profile: {jex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Error loading profile: {ex.Message}");
                return null;
            }
        }

        public static void SaveProfile(string filePath, ProfileData data)
        {
            try
            {
                string jsonString = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(filePath, jsonString);
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Error saving profile: {ex.Message}");
                throw;
            }
        }

        public static async Task SaveHistoryAsync(string historyFolder, string projectName, ProfileData data)
        {
            try
            {
                string newJson = JsonConvert.SerializeObject(data, Formatting.Indented);
                await Task.Run(() =>
                {
                    var existingFiles = new DirectoryInfo(historyFolder).GetFiles("*.json");
                    foreach (var file in existingFiles)
                    {
                        try
                        {
                            string existingJson = File.ReadAllText(file.FullName);
                            if (string.Equals(newJson, existingJson, StringComparison.Ordinal))
                            {
                                file.Delete();
                                break;
                            }
                        }
                        catch { }
                    }

                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
                    string filename = $"{projectName} {timestamp}.json";
                    string fullPath = Path.Combine(historyFolder, filename);
                    File.WriteAllText(fullPath, newJson);

                    ManageHistoryLimit(historyFolder);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save history: {ex.Message}");
                OnError?.Invoke($"Failed to save history: {ex.Message}");
            }
        }

        private static void ManageHistoryLimit(string historyFolder)
        {
            var files = new DirectoryInfo(historyFolder).GetFiles("*.json")
                                                       .OrderByDescending(f => f.LastWriteTime)
                                                       .ToList();

            if (files.Count > MAX_HISTORY_FILES)
            {
                var filesToDelete = files.Skip(MAX_HISTORY_FILES);
                foreach (var file in filesToDelete)
                {
                    try { file.Delete(); } catch { }
                }
            }
        }
    }
}