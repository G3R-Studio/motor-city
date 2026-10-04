using System;
using System.Collections.Generic;
using System.Reflection;

namespace MotorCity.World
{
    internal static class AuthoredCityFieldCache
    {
        private static readonly Dictionary<Type, Dictionary<string, FieldInfo>> FieldCache = new();

        internal static FieldInfo FindField(
            Type type,
            string fieldName)
        {
            if (type == null ||
                string.IsNullOrEmpty(
                    fieldName))
            {
                return null;
            }

            if (!FieldCache.TryGetValue(
                    type,
                    out Dictionary<string, FieldInfo> typeCache))
            {
                typeCache =
                    new Dictionary<string, FieldInfo>(
                        StringComparer.Ordinal);

                FieldCache[
                    type] =
                    typeCache;
            }

            if (typeCache.TryGetValue(
                    fieldName,
                    out FieldInfo cached))
            {
                return
                    cached;
            }

            Type current =
                type;

            while (current != null)
            {
                FieldInfo field =
                    current.GetField(
                        fieldName,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic);

                if (field != null)
                {
                    typeCache[
                        fieldName] =
                        field;

                    return
                        field;
                }

                current =
                    current.BaseType;
            }

            typeCache[
                fieldName] =
                null;

            return
                null;
        }
    }
}
