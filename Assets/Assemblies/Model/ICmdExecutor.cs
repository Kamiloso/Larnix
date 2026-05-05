#nullable enable
using Larnix.Core.Vectors;
using System.Collections.Generic;
using System.Linq;

namespace Larnix.Model;

public enum CmdResult { Raw, Info, Log, Success, Warning, Error, Ignore, Clear }

public interface ICmdExecutor
{
    public (CmdResult, string) ExecuteCommand(string command, string? sender = null);

    public bool TryExecuteCommand(string command, out string message)
    {
        var (result, msg) = ExecuteCommand(command);
        message = msg;
        return result != CmdResult.Error;
    }

    public static void InsertParameters(ref string command, Dictionary<string, string> parameters)
    {
        // Sort keys by length in descending order to prevent partial replacement
        IEnumerable<string> keys = parameters.Keys
            .OrderByDescending(k => k.Length);

        foreach (var key in keys)
        {
            command = command.Replace(key, parameters[key]);
        }
    }

    public static Col32 ResultToCol32(CmdResult result)
    {
        return result switch
        {
            CmdResult.Log => Col32.White,
            CmdResult.Info => Col32.Cyan,
            CmdResult.Success => Col32.Green,
            CmdResult.Warning => Col32.Yellow,
            CmdResult.Error => Col32.Red,
            _ => Col32.White
        };
    }
}
