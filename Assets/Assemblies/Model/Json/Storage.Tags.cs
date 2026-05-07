#nullable enable

namespace Larnix.Model.Json;

public partial class Storage // .Tags
{
    private const string F_KEY = "__flags__";

    public bool HasFlag(char flag)
    {
        return this[F_KEY].String.Contains(flag);
    }

    public void AddFlag(char flag)
    {
        this[F_KEY].String += flag;
    }

    public bool RemoveFlag(char flag, bool greedy)
    {
        string flags = this[F_KEY].String;
        int idx = flags.IndexOf(flag);
        if (idx >= 0)
        {
            this[F_KEY].String = greedy
                ? flags.Replace(flag.ToString(), string.Empty)
                : flags.Remove(idx, 1);
            return true;
        }
        return false;
    }
}
