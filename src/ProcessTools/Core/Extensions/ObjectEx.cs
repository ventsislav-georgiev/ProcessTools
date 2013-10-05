#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;

#endregion

namespace ProcessTools.Core.Extensions
{
    public static class ObjectEx
    {
        private static readonly MethodInfo CloneMethod = typeof (Object).GetMethod("MemberwiseClone",
                                                                                   BindingFlags.NonPublic |
                                                                                   BindingFlags.Instance);

        /// <summary>
        ///   Check any object against != null
        /// </summary>
        /// <param name="obj">The object</param>
        /// <returns>
        ///   <c>true</c> if the object is not null, otherwise <c>false</c>
        /// </returns>
        public static bool HasValue(this object obj)
        {
            return obj != null;
        }

        public static Object CreateInstance(Type type)
        {
            object instance = FormatterServices.GetUninitializedObject(type);
            return instance;
        }

        public static T Clone<T>(this T obj)
        {
            using (var memoryStream = new MemoryStream())
            {
                var serializer = new BinaryFormatter();
                serializer.Serialize(memoryStream, obj);
                memoryStream.Position = 0;
                object copy = serializer.Deserialize(memoryStream);
                return (T) copy;
            }
        }

        public static T CloneXml<T>(this T obj)
        {
            using (var memoryStream = new MemoryStream())
            {
                var serializer = new XmlSerializer(typeof (T));
                serializer.Serialize(memoryStream, obj);
                memoryStream.Position = 0;
                object copy = serializer.Deserialize(memoryStream);
                return (T) copy;
            }
        }

        public static bool IsPrimitive(this Type type)
        {
            if (type == typeof (String)) return true;
            return (type.IsValueType & type.IsPrimitive);
        }

        public static Object Copy(this Object originalObject)
        {
            return InternalCopy(originalObject, new Dictionary<Object, Object>(new ReferenceEqualityComparer()));
        }

        private static Object InternalCopy(Object originalObject, IDictionary<Object, Object> visited)
        {
            if (originalObject == null) return null;
            Type typeToReflect = originalObject.GetType();
            if (IsPrimitive(typeToReflect)) return originalObject;
            if (visited.ContainsKey(originalObject)) return visited[originalObject];
            object cloneObject = CloneMethod.Invoke(originalObject, null);
            visited.Add(originalObject, cloneObject);
            CopyFields(originalObject, visited, cloneObject, typeToReflect);
            RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect);
            return cloneObject;
        }

        private static void RecursiveCopyBaseTypePrivateFields(object originalObject,
                                                               IDictionary<object, object> visited, object cloneObject,
                                                               Type typeToReflect)
        {
            if (typeToReflect.BaseType != null)
            {
                RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect.BaseType);
                CopyFields(originalObject, visited, cloneObject, typeToReflect.BaseType,
                           BindingFlags.Instance | BindingFlags.NonPublic, info => info.IsPrivate);
            }
        }

        private static void CopyFields(object originalObject, IDictionary<object, object> visited, object cloneObject,
                                       Type typeToReflect,
                                       BindingFlags bindingFlags =
                                           BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public |
                                           BindingFlags.FlattenHierarchy, Func<FieldInfo, bool> filter = null)
        {
            foreach (FieldInfo fieldInfo in typeToReflect.GetFields(bindingFlags))
            {
                if (filter != null && filter(fieldInfo) == false) continue;
                if (IsPrimitive(fieldInfo.FieldType)) continue;
                object originalFieldValue = fieldInfo.GetValue(originalObject);
                object clonedFieldValue = originalFieldValue == null ? null : InternalCopy(originalFieldValue, visited);
                fieldInfo.SetValue(cloneObject, clonedFieldValue);
                if (clonedFieldValue == null) continue;
                if (fieldInfo.FieldType.IsArray)
                {
                    Type arrayType = fieldInfo.FieldType.GetElementType();
                    if (IsPrimitive(arrayType)) continue;
                    var clonedArray = (Array) clonedFieldValue;
                    for (long i = 0; i < clonedArray.LongLength; i++)
                        clonedArray.SetValue(InternalCopy(clonedArray.GetValue(i), visited), i);
                }
            }
        }

        public static T Copy<T>(this T original)
        {
            return (T) Copy((Object) original);
        }
    }

    public class ReferenceEqualityComparer : EqualityComparer<Object>
    {
        public override bool Equals(object x, object y)
        {
            return ReferenceEquals(x, y);
        }

        public override int GetHashCode(object obj)
        {
            if (obj == null) return 0;
            return obj.GetHashCode();
        }
    }
}