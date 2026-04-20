using System.Drawing;

public class Enemy
{
    public string Name { get; set; } = "Enemy";
    public char Symbol { get; set; } = 'E';
    public float X { get; set; }
    public float Y { get; set; }
    public float Angle { get; set; }
    public float SightDistance { get; set; }
    public float FovRadians { get; set; }
    public bool Alerted { get; set; }
    public Color Color { get; set; } = Color.Gray;
}