using System;
using System.IO;
using UnityEngine;

public static class GameSaveSystem
{
    private static readonly string SaveFilePath =
        Path.Combine(Application.persistentDataPath, "game-save.json");

    public static bool Save(GameSaveData saveData)
    {
        if (saveData == null)
        {
            Debug.LogError("Cannot save the game because the save data is null.");
            return false;
        }

        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(SaveFilePath, JsonUtility.ToJson(saveData, true));
            return true;
        }
        catch (IOException exception)
        {
            Debug.LogError($"Failed to write the game save: {exception.Message}");
            return false;
        }
        catch (UnauthorizedAccessException exception)
        {
            Debug.LogError($"Permission denied while writing the game save: {exception.Message}");
            return false;
        }
        catch (ArgumentException exception)
        {
            Debug.LogError($"Invalid game save data: {exception.Message}");
            return false;
        }
    }

    public static bool DeleteSave()
    {
        if (!File.Exists(SaveFilePath))
        {
            return true;
        }

        try
        {
            File.Delete(SaveFilePath);
            return true;
        }
        catch (IOException exception)
        {
            Debug.LogError($"Failed to delete the game save: {exception.Message}");
            return false;
        }
        catch (UnauthorizedAccessException exception)
        {
            Debug.LogError($"Permission denied while deleting the game save: {exception.Message}");
            return false;
        }
        catch (ArgumentException exception)
        {
            Debug.LogError($"Invalid game save path: {exception.Message}");
            return false;
        }
    }

    public static bool TryLoad(string sceneName, out GameSaveData saveData)
    {
        saveData = null;
        if (!File.Exists(SaveFilePath))
        {
            return false;
        }

        string json;
        try
        {
            json = File.ReadAllText(SaveFilePath);
        }
        catch (IOException exception)
        {
            Debug.LogError($"Failed to read the game save: {exception.Message}");
            return false;
        }
        catch (UnauthorizedAccessException exception)
        {
            Debug.LogError($"Permission denied while reading the game save: {exception.Message}");
            return false;
        }

        try
        {
            saveData = JsonUtility.FromJson<GameSaveData>(json);
        }
        catch (ArgumentException exception)
        {
            Debug.LogError($"The game save could not be parsed: {exception.Message}");
            return false;
        }

        if (saveData == null ||
            string.IsNullOrEmpty(saveData.sceneName) ||
            float.IsNaN(saveData.playerX) || float.IsInfinity(saveData.playerX) ||
            float.IsNaN(saveData.playerY) || float.IsInfinity(saveData.playerY) ||
            float.IsNaN(saveData.playerZ) || float.IsInfinity(saveData.playerZ))
        {
            Debug.LogError("The game save is missing required data or contains invalid player coordinates.");
            saveData = null;
            return false;
        }

        if (!string.Equals(saveData.sceneName, sceneName, StringComparison.Ordinal))
        {
            saveData = null;
            return false;
        }

        return true;
    }
}
