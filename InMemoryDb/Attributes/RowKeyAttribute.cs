using System;

namespace InMemoryDb
{
    /// <summary>
    /// Allows specifying the increment row key.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class RowKeyAttribute : Attribute
    {
    }
}
