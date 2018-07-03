using System;
using System.Reflection;

namespace InMemoryDb
{
    internal static class MemberInfoExtensions
    {
        /// <summary>
        /// Sets the property or the field value of a specified object.
        /// </summary>
        /// <param name="memberInfo">The member of the object to set new value to.</param>
        /// <param name="obj">The object whose property or field value will be set.</param>
        /// <param name="value">The new property or field value.</param>
        public static void SetValue(this MemberInfo memberInfo, object obj, object value)
        {
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Field:
                    ((FieldInfo)memberInfo).SetValue(obj, value);
                    break;
                case MemberTypes.Property:
                    ((PropertyInfo)memberInfo).SetValue(obj, value);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
