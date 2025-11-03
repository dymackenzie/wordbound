using Godot;
using System;

public interface IFileRepository
{
    /// <summary>
    /// Check if a file exists at the given path.
    /// </summary>
    bool Exists(string path);

    /// <summary>
    /// Save JSON content to a file at the given path.
    /// </summary>
    void Save(string path, string json);

    /// <summary>
    /// Load JSON content from a file at the given path.
    /// </summary>
    string Load(string path);

    /// <summary>
    /// Save content to a temporary file and then overwrite the original file.
    /// </summary>
    void SaveToFileAndOverwrite(string path, string tmpPath, string content);
}

public sealed class FileRepository : IFileRepository
{
    public bool Exists(string path)
    {
        return FileAccess.FileExists(path);
    }

    public void Save(string path, string json)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write); // truncates
        file.StoreString(json);
        file.Close();
    }

    public string Load(string path)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        var txt = file.GetAsText();
        file.Close();
        return txt;
    }

    public void SaveToFileAndOverwrite(string path, string tmpPath, string content)
    {
        Save(tmpPath, content);

        if (FileAccess.FileExists(path))
        {
            DirAccess.RemoveAbsolute(path);
        }
        DirAccess.RenameAbsolute(tmpPath, path);
    }
}
