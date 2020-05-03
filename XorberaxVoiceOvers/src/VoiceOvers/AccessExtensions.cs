using System;
using System.Reflection;

namespace VoiceOvers
{
    internal static class AccessExtensions
    {
        public static object Call(this object obj, string methodName, params object[] args)
        {
            var methodInfo = obj.GetType().GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (methodInfo != null)
            {
                return methodInfo.Invoke(obj, args);
            }
            return null;
        }

        public static object GetFieldValue(this object instance, string fieldName)
        {
            var bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            var field = instance.GetType().GetField(fieldName, bindFlags);
            return field.GetValue(instance);
        }

        public static object GetField(Type type, string name)
        {
            var info = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
            return info?.GetValue(null);
        }
    }
}
