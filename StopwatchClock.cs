using System;

public class StopwatchClock
{
    private DateTime last = DateTime.Now;

    public void Start()
    {
        last = DateTime.Now;
    }

    public float Step()
    {
        DateTime now = DateTime.Now;
        float dt = (float)(now - last).TotalSeconds;
        last = now;
        if (dt < 0.001f) return 0.001f;
        if (dt > 0.05f) return 0.05f;
        return dt;
    }
}