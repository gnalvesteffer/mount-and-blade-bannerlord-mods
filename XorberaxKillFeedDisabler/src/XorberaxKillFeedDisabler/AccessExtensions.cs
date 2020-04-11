namespace KillFeedDisabler
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
    }
}
