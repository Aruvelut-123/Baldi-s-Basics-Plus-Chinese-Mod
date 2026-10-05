using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BBPC.Compat
{
    /// <summary>
    /// Reflection helpers for optional extension assemblies.
    /// Positive lookups are cached because these methods run during menu updates.
    /// </summary>
    internal static class CompatReflection
    {
        private const BindingFlags StaticFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        private const BindingFlags InstanceFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static readonly object CacheLock = new object();
        private static readonly Dictionary<string, Assembly> AssemblyCache = new Dictionary<string, Assembly>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Type> TypeCache = new Dictionary<string, Type>(StringComparer.Ordinal);
        private static readonly Dictionary<string, MemberInfo> MemberCache = new Dictionary<string, MemberInfo>(StringComparer.Ordinal);

        public static Assembly? FindAssembly(string assemblyName)
        {
            if (string.IsNullOrEmpty(assemblyName))
            {
                return null;
            }

            lock (CacheLock)
            {
                if (AssemblyCache.TryGetValue(assemblyName, out Assembly cachedAssembly))
                {
                    return cachedAssembly;
                }
            }

            Assembly? assembly;
            try
            {
                assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(candidate => string.Equals(candidate.GetName().Name, assemblyName, StringComparison.Ordinal));
            }
            catch
            {
                return null;
            }

            if (assembly != null)
            {
                lock (CacheLock)
                {
                    AssemblyCache[assemblyName] = assembly;
                }
            }

            return assembly;
        }

        public static Type? FindType(string assemblyName, string typeFullName)
        {
            if (string.IsNullOrEmpty(typeFullName))
            {
                return null;
            }

            string cacheKey = assemblyName + "\0" + typeFullName;
            lock (CacheLock)
            {
                if (TypeCache.TryGetValue(cacheKey, out Type cachedType))
                {
                    return cachedType;
                }
            }

            Assembly? assembly = FindAssembly(assemblyName);
            Type? type;
            try
            {
                type = assembly?.GetType(typeFullName, false);
            }
            catch
            {
                return null;
            }

            if (type != null)
            {
                lock (CacheLock)
                {
                    TypeCache[cacheKey] = type;
                }
            }

            return type;
        }

        public static Type? FindTypeBySimpleName(string assemblyName, string simpleName)
        {
            if (string.IsNullOrEmpty(simpleName))
            {
                return null;
            }

            string cacheKey = assemblyName + "\0*" + simpleName;
            lock (CacheLock)
            {
                if (TypeCache.TryGetValue(cacheKey, out Type cachedType))
                {
                    return cachedType;
                }
            }

            Assembly? assembly = FindAssembly(assemblyName);
            if (assembly == null)
            {
                return null;
            }

            IEnumerable<Type> types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(type => type != null).Cast<Type>();
            }
            catch
            {
                return null;
            }

            Type? type = types.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, simpleName, StringComparison.Ordinal));
            if (type != null)
            {
                lock (CacheLock)
                {
                    TypeCache[cacheKey] = type;
                }
            }

            return type;
        }

        public static object? GetStatic(object? target, string name)
        {
            Type? type = target as Type ?? target?.GetType();
            MemberInfo? member = type == null ? null : FindMember(type, name, true);
            return ReadMember(member, null);
        }

        public static object? GetInstance(object? target, string name)
        {
            if (target == null)
            {
                return null;
            }

            MemberInfo? member = FindMember(target.GetType(), name, false);
            return ReadMember(member, target);
        }

        public static int GetCount(object? collection)
        {
            if (collection == null)
            {
                return 0;
            }

            if (collection is ICollection nonGenericCollection)
            {
                return nonGenericCollection.Count;
            }

            object? count = ReadMember(FindMember(collection.GetType(), "Count", false), collection);
            return count == null ? 0 : Convert.ToInt32(count);
        }

        private static MemberInfo? FindMember(Type type, string name, bool isStatic)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            string cacheKey = (isStatic ? "S" : "I") + "\0" +
                              (type.AssemblyQualifiedName ?? type.FullName ?? type.Name) + "\0" + name;
            lock (CacheLock)
            {
                if (MemberCache.TryGetValue(cacheKey, out MemberInfo cachedMember))
                {
                    return cachedMember;
                }
            }

            BindingFlags flags = isStatic ? StaticFlags : InstanceFlags;
            MemberInfo? member = null;
            for (Type? current = type; current != null && member == null; current = current.BaseType)
            {
                member = current.GetField(name, flags) as MemberInfo ?? current.GetProperty(name, flags);
            }

            if (member != null)
            {
                lock (CacheLock)
                {
                    MemberCache[cacheKey] = member;
                }
            }

            return member;
        }

        private static object? ReadMember(MemberInfo? member, object? target)
        {
            try
            {
                if (member is FieldInfo field)
                {
                    return field.GetValue(target);
                }

                if (member is PropertyInfo property && property.GetGetMethod(true) != null)
                {
                    return property.GetValue(target, null);
                }
            }
            catch
            {
                // Optional members are expected to vary between extension versions.
            }

            return null;
        }
    }
}
