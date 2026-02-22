using System.Collections.Generic;
using CmdResult = Larnix.Core.ICmdExecutor.CmdResult;
using System;
using System.Text;
using System.Linq;
using Larnix.Server.Commands.All;
using Larnix.Packets;

namespace Larnix.Server.Commands
{
    internal abstract class BaseCmd
    {
        protected BaseCmd() { }
        static BaseCmd()
        {
            // Register in alphabetical order
            // for no particular reason other than aesthetics of the source code.

            lock (_lock)
            {
                RegisterCommand<Clear>();
                RegisterCommand<Fill>();
                RegisterCommand<Help>();
                RegisterCommand<Info>();
                RegisterCommand<Kick>();
                RegisterCommand<Kill>();
                RegisterCommand<Particles>();
                RegisterCommand<Playerlist>();
                RegisterCommand<Replace>();
                RegisterCommand<Spawn>();
                RegisterCommand<Stop>();
                RegisterCommand<Tp>();
            }
        }

        public string Name => GetType().Name.ToLowerInvariant();
        public string ShortDocLine => Pattern + " - " + ShortDescription;
        public string InvalidCmdFormat => $"Invalid format! Usage: {Pattern}";
        public string Documentation =>
            $"Command: {Name}\n" +
            $"Privilege: {PrivilegeLevel}\n" +
            $"Usage: {Pattern}\n" +
            $"\n" +
            $"{LongDescription}";

        public abstract PrivilegeLevel PrivilegeLevel { get; }
        public abstract string Pattern { get; }
        public abstract string ShortDescription { get; }
        public virtual string LongDescription => ShortDescription;

        public abstract void Inject(string command);
        public abstract (CmdResult, string) Execute(string sender, PrivilegeLevel privilege);

        public override string ToString() => "LarnixCommand::" + Name;

        protected static bool TrySplit(string command, int expectedLength, out string[] parts,
            bool lastJoin = false)
        {
            string[] split = command.Split(' ');
            int length = split.Length;
            
            if (length < expectedLength)
            {
                parts = default;
                return false;
            }

            if (length == expectedLength)
            {
                parts = split;
                return true;
            }

            if (length > expectedLength && lastJoin)
            {
                parts = new string[expectedLength];
                Array.Copy(split, parts, expectedLength - 1);
                parts[expectedLength - 1] = string.Join(' ', split.Skip(expectedLength - 1));
                return true;
            }

            parts = default;
            return false;
        }

        protected static string MakeRobustList(string title, IEnumerable<string> lines)
        {
            StringBuilder sb = new();
            sb.AppendLine();
            sb.AppendLine($" | ------ {title} ------");
            sb.AppendLine($" |");
            
            foreach (string line in lines)
            {
                sb.AppendLine(" | " + line);
            }

            sb.AppendLine();
            return sb.ToString();
        }

        protected FormatException FormatException(string errorInfo)
        {
            return new FormatException(errorInfo);
        }

#region Static factory

        private static readonly Dictionary<string, BaseCmd> _cmdObjs = new();
        private static readonly object _lock = new();

        public static bool TryCreateCommandObject(string cmd, out BaseCmd cmdObj)
        {
            lock (_lock)
            {
                if (_cmdObjs.TryGetValue(cmd, out cmdObj))
                {
                    cmdObj = (BaseCmd)cmdObj.MemberwiseClone();
                    return true;
                }
                
                cmdObj = default;
                return false;
            }
        }

        protected static IEnumerable<string> AllCommandDocs(PrivilegeLevel privilege)
        {
            lock (_lock)
            {
                List<string> lines = new();
                foreach (var cmdObj in _cmdObjs.Values.OrderBy(c => c.Name))
                {
                    if (cmdObj.PrivilegeLevel <= privilege)
                    {
                        lines.Add(cmdObj.ShortDocLine);
                    }
                }
                return lines;
            }
        }

        protected static bool TryGetDocumentation(string cmd, PrivilegeLevel privilege,
            out string documentation)
        {
            lock (_lock)
            {
                if (_cmdObjs.TryGetValue(cmd, out var cmdObj))
                {
                    if (cmdObj.PrivilegeLevel <= privilege)
                    {
                        documentation = cmdObj.Documentation;
                        return true;
                    }
                }

                documentation = default;
                return false;
            }
        }

        private static void RegisterCommand<T>() where T : BaseCmd, new()
        {
            lock (_lock)
            {
                string cmd = typeof(T).Name.ToLower();

                T original = new T();
                if (!_cmdObjs.TryAdd(cmd, original))
                {
                    throw new InvalidOperationException($"Command {cmd} is already registered.");
                }
            }
        }

#endregion

    }
}
