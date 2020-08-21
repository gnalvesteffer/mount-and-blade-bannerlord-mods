namespace Banks
{
    public static class OverflowUtility
    {
        public static bool WillAdditionOverflow(int a, int b)
        {
            return a > 0 && b > 0 && b > int.MaxValue - a || a < 0 && b < 0 && b < int.MinValue - a;
        }
    }
}
