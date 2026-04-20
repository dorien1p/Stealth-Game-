using System;
using System.Drawing;
using System.Windows.Forms;

public static class RenderHelpers
{
    public static Color GetWallColor(float distance, bool boundary)
    {
        if (boundary) return Color.Black;
        if (distance < 130f) return Color.FromArgb(210, 210, 220);
        if (distance < 240f) return Color.FromArgb(170, 170, 180);
        if (distance < 360f) return Color.FromArgb(130, 130, 140);
        if (distance < 520f) return Color.FromArgb(95, 95, 104);
        return Color.FromArgb(62, 62, 68);
    }

    public static Color GetFloorColor(float brightness)
    {
        brightness = Math.Clamp(brightness, 0f, 1f);
        int value = (int)(35 + brightness * 65);
        return Color.FromArgb(value, value - 4, value - 8);
    }

    public static void RenderEnemyBody(Graphics g, Enemy enemy, int left, int top, int bodyWidth, int bodyHeight, Font hudFont)
    {
        Color bodyColor = enemy.Alerted ? Color.OrangeRed : enemy.Color;
        Color outlineColor = enemy.Alerted ? Color.White : Color.Black;

        int headSize = Math.Max(10, bodyWidth / 3);
        Rectangle head = new(left + bodyWidth / 2 - headSize / 2, top + bodyHeight / 12, headSize, headSize);

        int torsoWidth = Math.Max(12, bodyWidth / 2);
        int torsoHeight = Math.Max(18, bodyHeight / 3);
        Rectangle torso = new(left + bodyWidth / 2 - torsoWidth / 2, head.Bottom - 2, torsoWidth, torsoHeight);

        int armWidth = Math.Max(4, bodyWidth / 7);
        int armHeight = Math.Max(14, torsoHeight - 4);
        Rectangle leftArm = new(torso.Left - armWidth + 2, torso.Top + 4, armWidth, armHeight);
        Rectangle rightArm = new(torso.Right - 2, torso.Top + 4, armWidth, armHeight);

        int legWidth = Math.Max(5, torsoWidth / 3);
        int legHeight = Math.Max(16, bodyHeight / 3);
        Rectangle leftLeg = new(torso.Left + 2, torso.Bottom - 2, legWidth, legHeight);
        Rectangle rightLeg = new(torso.Right - legWidth - 2, torso.Bottom - 2, legWidth, legHeight);

        using SolidBrush shadowBrush = new(Color.FromArgb(90, 0, 0, 0));
        using SolidBrush bodyBrush = new(bodyColor);
        using SolidBrush darkBrush = new(ControlPaint.Dark(bodyColor));
        using SolidBrush faceBrush = new(Color.FromArgb(230, 215, 190));
        using Pen outlinePen = new(outlineColor, Math.Max(1f, bodyWidth / 14f));
        using Pen detailPen = new(Color.Black, 1.5f);
        using SolidBrush eyeBrush = new(Color.Black);
        using Font symbolFont = new("Consolas", Math.Max(8, bodyWidth / 4), FontStyle.Bold);

        Rectangle shadow = new(left + bodyWidth / 5, top + bodyHeight - 8, bodyWidth * 3 / 5, 10);
        g.FillEllipse(shadowBrush, shadow);

        g.FillRectangle(darkBrush, leftArm);
        g.FillRectangle(darkBrush, rightArm);
        g.FillRectangle(darkBrush, leftLeg);
        g.FillRectangle(darkBrush, rightLeg);
        g.DrawRectangle(outlinePen, leftArm);
        g.DrawRectangle(outlinePen, rightArm);
        g.DrawRectangle(outlinePen, leftLeg);
        g.DrawRectangle(outlinePen, rightLeg);

        g.FillRectangle(bodyBrush, torso);
        g.DrawRectangle(outlinePen, torso);

        g.FillEllipse(faceBrush, head);
        g.DrawEllipse(outlinePen, head);

        int eyeSize = Math.Max(2, headSize / 9);
        int eyeY = head.Y + head.Height / 3;
        g.FillEllipse(eyeBrush, head.X + head.Width / 4 - eyeSize / 2, eyeY, eyeSize, eyeSize);
        g.FillEllipse(eyeBrush, head.Right - head.Width / 4 - eyeSize / 2, eyeY, eyeSize, eyeSize);
        g.DrawArc(detailPen, head.X + head.Width / 3, head.Y + head.Height / 2, head.Width / 3, head.Height / 5, 10, 160);

        StringFormat centered = new()
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        g.DrawString(enemy.Symbol.ToString(), symbolFont, Brushes.White, torso, centered);

        if (enemy.Alerted)
        {
            using Pen alertPen = new(Color.Yellow, Math.Max(2f, bodyWidth / 10f));
            int exclamationX = left + bodyWidth / 2;
            int exclamationY = top - Math.Max(16, bodyWidth / 3);
            g.DrawLine(alertPen, exclamationX, exclamationY, exclamationX, exclamationY + 12);
            g.FillEllipse(Brushes.Yellow, exclamationX - 2, exclamationY + 16, 4, 4);
        }
    }
}