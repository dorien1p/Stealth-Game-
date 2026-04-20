using System;

public static class MathHelpers
{
    public static float Distance(float x1, float y1, float x2, float y2)
    {
        float dx = x2 - x1;
        float dy = y2 - y1;
        return (float)Math.Sqrt(dx * dx + dy * dy);
    }

    public static float RotateToward(float current, float target, float maxStep)
    {
        float diff = NormalizeAngle(target - current);
        if (Math.Abs(diff) <= maxStep) return target;
        return current + Math.Sign(diff) * maxStep;
    }

    public static float NormalizeAngle(float angle)
    {
        while (angle < -(float)Math.PI) angle += (float)(Math.PI * 2);
        while (angle > (float)Math.PI) angle -= (float)(Math.PI * 2);
        return angle;
    }

    public static float DegreesToRadians(float degrees)
    {
        return degrees * (float)Math.PI / 180f;
    }
}