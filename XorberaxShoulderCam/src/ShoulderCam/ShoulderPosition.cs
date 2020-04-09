namespace ShoulderCam
{
    internal enum ShoulderPosition
    {
        Left,
        Right,
    }

    internal static class ShoulderPositionExtensions
    {
        public static float GetOffsetValue(this ShoulderPosition shoulderPosition)
        {
            switch (shoulderPosition)
            {
                case ShoulderPosition.Left:
                    return -1;
                case ShoulderPosition.Right:
                    return 1;
                default:
                    return 0;
            }
        }
    }
}
