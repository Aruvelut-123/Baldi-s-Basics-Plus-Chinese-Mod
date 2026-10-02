using System;
using System.Linq;
using System.Reflection;

using BBPC.API;

namespace BBPC.Compat
{
    /// <summary>
    /// 反射辅助工具：用于在 main 中检测外部扩展模组程序集/类型是否存在�?    /// 这样 main 编译时无需引用任何模组 dll（纯软兼容）�?    /// </summary>
    internal static class CompatReflection
    {
        /// <summary>在已加载程序集中按程序集名查找程序集�?/summary>
        public static Assembly? FindAssembly(string assemblyName)
        {
            try
            {
                return AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == assemblyName);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>按全名查找类型，例如 "ChallengeJar.Menu.ChallengeExtraMenu"�?/summary>
        public static Type? FindType(string assemblyName, string typeFullName)
        {
            Assembly? asm = FindAssembly(assemblyName);
            if (asm == null) return null;
            try
            {
                return asm.GetType(typeFullName, false);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>按简单类型名（忽略命名空间）在指定程序集中查找类型�?/summary>
        public static Type? FindTypeBySimpleName(string assemblyName, string simpleName)
        {
            Assembly? asm = FindAssembly(assemblyName);
            if (asm == null) return null;
            try
            {
                return asm.GetTypes().FirstOrDefault(t => t.Name == simpleName);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>读取静态字�?属性（target �?Type �?null）�?/summary>
        public static object? GetStatic(object? target, string name)
        {
            Type? type = target as Type ?? target?.GetType();
            if (type == null) return null;
            try
            {
                FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (field != null) return field.GetValue(null);
                PropertyInfo? prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                return prop?.GetValue(null, null);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>读取实例字段/属性�?/summary>
        public static object? GetInstance(object target, string name)
        {
            if (target == null) return null;
            Type type = target.GetType();
            try
            {
                FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null) return field.GetValue(target);
                PropertyInfo? prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                return prop?.GetValue(target, null);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>读取集合/数组�?Count/Length�?/summary>
        public static int GetCount(object collection)
        {
            if (collection == null) return 0;
            try
            {
                if (collection is System.Collections.ICollection iCol) return iCol.Count;
                PropertyInfo? countProp = collection.GetType().GetProperty("Count");
                return countProp == null ? 0 : Convert.ToInt32(countProp.GetValue(collection, null));
            }
            catch
            {
                return 0;
            }
        }
    }
}

