using System;

namespace InMemoryDb
{
    /// <summary>
    /// Allows specifying the origin data identificator.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public class OriginNameAttribute : Attribute
    {
        /// <summary>
        /// Gets the table name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes new instance of <see cref="OriginNameAttribute"/>
        /// </summary>
        /// <param name="name">Table name.</param>
        public OriginNameAttribute(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}
