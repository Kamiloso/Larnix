#nullable enable
using Larnix.Core;
using Larnix.Core.Files;
using Larnix.Server.Run.Records;

namespace Larnix.Server.Data;

internal interface IConfigSaver
{
    void Save();
}

internal class ConfigSaver : IConfigSaver
{
    private Config Config => GlobRef.Get<Config>();
    private RunInfo RunInfo => GlobRef.Get<RunInfo>();

    private readonly string _configFile;

    public ConfigSaver(string configFile)
    {
        _configFile = configFile;

        string path = RunInfo.SavesPath;
        string file = _configFile;

        string? json = FileManager.Read(path, file);
        if (json != null)
        {
            Config config = new(json);
            GlobRef.Set(config);
        }
    }

    public void Save()
    {
        string path = RunInfo.SavesPath;
        string file = _configFile;

        string json = Config.ToJson();
        FileManager.Write(path, file, json);
    }
}
