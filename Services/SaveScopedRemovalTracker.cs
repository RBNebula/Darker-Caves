using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using DarkCaves.Configuration;

namespace DarkCaves.Services;

internal sealed class SaveScopedRemovalTracker
{
    private readonly ManualLogSource _logger;
    private readonly string _stateFilePath;
    private readonly HashSet<string> _processedSaveKeys = new(StringComparer.OrdinalIgnoreCase);

    public SaveScopedRemovalTracker(DarkCavesConfig config, ManualLogSource logger)
    {
        _logger = logger;
        _stateFilePath = BuildStateFilePath(config.SaveScopedStateFileName);
        LoadState();
    }

    public string GetActiveSaveKey()
    {
        SavingLoadingManager? manager = SavingLoadingManager.Instance;
        if (manager == null)
        {
            return string.Empty;
        }

        return NormalizeSaveKey(manager.ActiveSaveFileName);
    }

    public bool HasProcessed(string saveKey)
    {
        string normalized = NormalizeSaveKey(saveKey);
        if (normalized.Length == 0)
        {
            return false;
        }

        return _processedSaveKeys.Contains(normalized);
    }

    public bool MarkProcessed(string saveKey)
    {
        string normalized = NormalizeSaveKey(saveKey);
        if (normalized.Length == 0)
        {
            return false;
        }

        if (!_processedSaveKeys.Add(normalized))
        {
            return false;
        }

        SaveState();
        return true;
    }

    private void LoadState()
    {
        if (!File.Exists(_stateFilePath))
        {
            return;
        }

        try
        {
            string[] lines = File.ReadAllLines(_stateFilePath);
            for (int i = 0; i < lines.Length; i++)
            {
                string normalized = NormalizeSaveKey(lines[i]);
                if (normalized.Length > 0)
                {
                    _processedSaveKeys.Add(normalized);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed reading save-scoped state file '{_stateFilePath}': {ex.Message}");
        }
    }

    private void SaveState()
    {
        try
        {
            string? directory = Path.GetDirectoryName(_stateFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string[] lines = _processedSaveKeys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToArray();
            File.WriteAllLines(_stateFilePath, lines);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed writing save-scoped state file '{_stateFilePath}': {ex.Message}");
        }
    }

    private static string BuildStateFilePath(string configuredFileName)
    {
        string fileName = (configuredFileName ?? string.Empty).Trim();
        if (fileName.Length == 0)
        {
            fileName = "DarkCaves.saveScopedRemoval.state";
        }

        if (Path.IsPathRooted(fileName))
        {
            return fileName;
        }

        return Path.Combine(Paths.ConfigPath, fileName);
    }

    private static string NormalizeSaveKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string trimmed = value!.Trim();
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(trimmed);
        string normalized = string.IsNullOrWhiteSpace(fileNameWithoutExtension) ? trimmed : fileNameWithoutExtension;
        return normalized.Trim();
    }
}
