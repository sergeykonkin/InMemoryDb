using System;

namespace InMemoryDb
{
    /// <summary>
    /// Allows specifying the SQL table name (including schema).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TableAttribute : Attribute
    {
        /// <summary>
        /// Gets the table name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes new instance of <see cref="TableAttribute" />
        /// </summary>
        /// <param name="name">Table name.</param>
        public TableAttribute(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}
