using System;
using System.Collections.Generic;
using System.Linq;

namespace Larnix.Core
{
    public interface ICmdExecutor
    {
        public enum CmdResult { Raw, Log, Success, Warning, Error, Ignore }

        public (CmdResult, string) ExecuteCommand(string command, string sender = null);
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
    }
}
