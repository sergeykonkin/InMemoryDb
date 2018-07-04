using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace InMemoryDb
{
    internal static class ReflectionHelper
    {
        private static readonly IDictionary<Type, Tuple<MemberInfo, string>[]> _columnMapCache = new ConcurrentDictionary<Type, Tuple<MemberInfo, string>[]>();
        private static readonly IDictionary<Type, string> _tableNameCache = new ConcurrentDictionary<Type, string>();
        private static readonly IDictionary<Type, MemberInfo[]> _writableMembersCache = new ConcurrentDictionary<Type, MemberInfo[]>();

        public static Tuple<MemberInfo, string>[] GetColumnMap(Type type)
        {
            Tuple<MemberInfo, string>[] GetColumnMapImpl(Type t)
            {
                return GetWritableMembers(t)
                    .Where(member => member.GetCustomAttribute<NotMappedAttribute>() == null)
                    .Select(
                        member => new Tuple<MemberInfo, string>(
                            member,
                            member.GetCustomAttribute<ColumnAttribute>()?.Name ?? member.Name))
                    .ToArray();
            }

            if (_columnMapCache.ContainsKey(type))
            {
                return _columnMapCache[type];
            }

            var columnMap = GetColumnMapImpl(type);

            _columnMapCache.Add(type, columnMap);
            return columnMap;
        }

        public static string GetTableName(Type type)
        {
            string GetTableNameImpl(Type t)
            {
                var tableAttr = t.GetCustomAttribute<TableAttribute>();
                if (tableAttr == null)
                {
                    return t.Name;
                }

                return (tableAttr.Schema == null ? "" : tableAttr.Schema + ".")
                       + tableAttr.Name;
            }

            if (_tableNameCache.ContainsKey(type))
            {
                return _tableNameCache[type];
            }

            var name = GetTableNameImpl(type);
            _tableNameCache[type] = name;
            return name;
        }

        public static MemberInfo[] GetWritableMembers(Type type)
        {
            MemberInfo[] GetWritableMembersImpl(Type t)
            {
                return t
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(prop => prop.CanWrite)
                    .Cast<MemberInfo>()
                    .Union(type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                    .ToArray();
            }

            if (_writableMembersCache.ContainsKey(type))
            {
                return _writableMembersCache[type];
            }

            var members = GetWritableMembersImpl(type);
            _writableMembersCache[type] = members;
            return members;
        }
    }
}
