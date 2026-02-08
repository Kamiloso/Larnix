using System;

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
    }
}
