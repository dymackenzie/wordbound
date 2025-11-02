using Godot;
using System;

public sealed class FileRepository
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

    public void WriteTempAndReplace(string path, string tmpPath, string content)
    {
        Save(path, content);
    }
}
