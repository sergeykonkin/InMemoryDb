using System;

namespace InMemoryDb
{
    /// <summary>
    /// Allows to ignore property from auto-mapping.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class IgnoreAttribute : Attribute
    {
    }
}
