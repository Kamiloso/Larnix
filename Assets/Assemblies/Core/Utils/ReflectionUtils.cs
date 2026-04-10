#nullable enable
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Larnix.Core.Utils;

public static class ReflectionUtils
{
    public static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.Where(t => t != null)!;
        }
    }
}
