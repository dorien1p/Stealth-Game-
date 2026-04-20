using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

public class GameForm : Form
{
    private bool mouseLookEnabled = true;
private Point screenCenter;
private bool recenteringMouse = false;
private const float MouseSensitivity = 0.0035f;
    private const int ScreenWidth = 1280;
    private const int ScreenHeight = 720;
    private const int MiniMapScale = 12;
    private const float Fov = (float)(Math.PI / 3.0);
    private const float Depth = 1200f;
    private const float MoveSpeed = 140f;
    private const float TurnSpeed = 2.2f;
    private const float EnemySpeed = 65f;
    private const float CaptureDistance = 14f;
    private const float TileSize = 32f;

    private readonly string[] mapRows =
    {
        "##############################################",
        "##$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$##",
        "##$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$##",
        "##$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$##",
        "##$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$##",
        "##$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$##",
        "##$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$##",
        "##$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$##",
        "##$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$##",
        "##$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$##",
        "##$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$##",
        "##############################################"
    };

    private readonly System.Windows.Forms.Timer gameTimer = new();
    private readonly HashSet<Keys> pressedKeys = new();
    private readonly StopwatchClock clock = new();
    private readonly Font hudFont = new("Consolas", 10, FontStyle.Bold);
    private readonly Font titleFont = new("Consolas", 20, FontStyle.Bold);

    private char[,] world = null!;
    private int mapWidth;
    private int mapHeight;
    private float[] depthBuffer = null!;

    private Player player = null!;
    private List<Enemy> enemies = null!;
    private float elapsedSeconds;
    private bool gameOver;
    private string gameOverReason = string.Empty;

public GameForm()
{
    Text = "Mull - First Person 3D Prototype";
    ClientSize = new Size(ScreenWidth, ScreenHeight);
    StartPosition = FormStartPosition.CenterScreen;
    DoubleBuffered = true;
    KeyPreview = true;
    BackColor = Color.Black;

    Cursor.Hide();
    UpdateMouseCenter();
    MouseMove += OnGameMouseMove;
    Resize += (_, _) => UpdateMouseCenter();
    Activated += (_, _) =>
    {
        Cursor.Hide();
        UpdateMouseCenter();
        CenterMouse();
    };
    Deactivate += (_, _) => Cursor.Show();

    BuildWorld();
    BuildActors();
    UpdateMouseCenter();
    depthBuffer = new float[ClientSize.Width];

    gameTimer.Interval = 20;
    gameTimer.Tick += (_, _) => GameLoop();
    clock.Start();
    gameTimer.Start();
}
private void UpdateMouseCenter()
{
    Point clientCenter = new Point(ClientSize.Width / 2, ClientSize.Height / 2);
    screenCenter = PointToScreen(clientCenter);
}

private void CenterMouse()
{
    recenteringMouse = true;
    Cursor.Position = screenCenter;
}

private void OnGameMouseMove(object? sender, MouseEventArgs e)
{
    if (!mouseLookEnabled || recenteringMouse)
    {
        recenteringMouse = false;
        return;
    }

    Point currentScreenPos = PointToScreen(e.Location);
    int deltaX = currentScreenPos.X - screenCenter.X;

    player.Angle += deltaX * MouseSensitivity;

    CenterMouse();
}
    private void BuildWorld()
    {
        mapHeight = mapRows.Length;
        mapWidth = mapRows.Max(r => r.Length);
        world = new char[mapWidth, mapHeight];

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                char c = x < mapRows[y].Length ? mapRows[y][x] : '#';
                world[x, y] = c == '$' ? '.' : '#';
            }
        }
    }

    private void BuildActors()
    {
        player = new Player
        {
            Name = "Mull",
            X = 6.5f * TileSize,
            Y = 6.0f * TileSize,
            Angle = 0f
        };

        enemies = new List<Enemy>
        {
            new Enemy
            {
                Name = "Watcher Wide",
                Symbol = 'W',
                X = 10.5f * TileSize,
                Y = 3.5f * TileSize,
                Angle = (float)Math.PI,
                SightDistance = 3f * TileSize,
                FovRadians = MathHelpers.DegreesToRadians(160f),
                Color = Color.Goldenrod
            },
            new Enemy
            {
                Name = "Sniper Eye",
                Symbol = 'S',
                X = 30.5f * TileSize,
                Y = 2.5f * TileSize,
                Angle = 0f,
                SightDistance = 50f * TileSize,
                FovRadians = MathHelpers.DegreesToRadians(12f),
                Color = Color.IndianRed
            },
            new Enemy
            {
                Name = "Guard 45",
                Symbol = 'G',
                X = 25.5f * TileSize,
                Y = 8.5f * TileSize,
                Angle = (float)Math.PI,
                SightDistance = 10f * TileSize,
                FovRadians = MathHelpers.DegreesToRadians(45f),
                Color = Color.MediumSeaGreen
            }
        };
    }

    private void GameLoop()
    {
        float dt = clock.Step();
        elapsedSeconds += dt;

        if (!gameOver)
        {
            UpdatePlayer(dt);
            UpdateEnemies(dt);
            CheckCapture();
        }

        Invalidate();
        if (Focused && mouseLookEnabled)
    CenterMouse();
    }

    private void UpdatePlayer(float dt)
    {
if (pressedKeys.Contains(Keys.Left))
    player.Angle -= TurnSpeed * dt;

if (pressedKeys.Contains(Keys.Right))
    player.Angle += TurnSpeed * dt;
        float moveStep = 0f;
        float strafeStep = 0f;

        if (pressedKeys.Contains(Keys.W)) moveStep += MoveSpeed * dt;
        if (pressedKeys.Contains(Keys.S)) moveStep -= MoveSpeed * dt;
        if (pressedKeys.Contains(Keys.A)) strafeStep -= MoveSpeed * dt;
        if (pressedKeys.Contains(Keys.D)) strafeStep += MoveSpeed * dt;

        float forwardX = (float)Math.Cos(player.Angle);
        float forwardY = (float)Math.Sin(player.Angle);
        float rightX = (float)Math.Cos(player.Angle + Math.PI / 2.0);
        float rightY = (float)Math.Sin(player.Angle + Math.PI / 2.0);

        float dx = forwardX * moveStep + rightX * strafeStep;
        float dy = forwardY * moveStep + rightY * strafeStep;
        TryMovePlayer(dx, dy);
    }

    private void UpdateEnemies(float dt)
    {
        foreach (Enemy enemy in enemies)
        {
            bool seesPlayer = CanSee(enemy.X, enemy.Y, enemy.Angle, enemy.FovRadians, enemy.SightDistance, player.X, player.Y);
            enemy.Alerted = seesPlayer;

            if (enemy.Alerted)
            {
                float targetAngle = (float)Math.Atan2(player.Y - enemy.Y, player.X - enemy.X);
                enemy.Angle = MathHelpers.RotateToward(enemy.Angle, targetAngle, TurnSpeed * dt * 1.3f);

                float dx = (float)Math.Cos(enemy.Angle) * EnemySpeed * dt;
                float dy = (float)Math.Sin(enemy.Angle) * EnemySpeed * dt;
                TryMoveEnemy(enemy, dx, dy);
            }
            else
            {
                enemy.Angle += 0.35f * dt;
            }
        }
    }

    private void CheckCapture()
    {
        foreach (Enemy enemy in enemies)
        {
            float d = MathHelpers.Distance(enemy.X, enemy.Y, player.X, player.Y);
            if (d <= CaptureDistance)
            {
                gameOver = true;
                gameOverReason = enemy.Name + " caught Mull.";
                return;
            }
        }
    }

    private void TryMovePlayer(float dx, float dy)
    {
        float newX = player.X + dx;
        float newY = player.Y + dy;

        if (IsWalkable(newX, player.Y)) player.X = newX;
        if (IsWalkable(player.X, newY)) player.Y = newY;
    }

    private void TryMoveEnemy(Enemy enemy, float dx, float dy)
    {
        float newX = enemy.X + dx;
        float newY = enemy.Y + dy;

        if (IsWalkable(newX, enemy.Y)) enemy.X = newX;
        if (IsWalkable(enemy.X, newY)) enemy.Y = newY;
    }

    private bool IsWalkable(float worldX, float worldY)
    {
        int tx = (int)(worldX / TileSize);
        int ty = (int)(worldY / TileSize);
        return InBounds(tx, ty) && world[tx, ty] != '#';
    }

    private bool InBounds(int x, int y)
    {
        return x >= 0 && x < mapWidth && y >= 0 && y < mapHeight;
    }

    private bool CanSee(float ex, float ey, float facingAngle, float fovRadians, float maxDist, float px, float py)
    {
        float dx = px - ex;
        float dy = py - ey;
        float dist = (float)Math.Sqrt(dx * dx + dy * dy);
        if (dist > maxDist) return false;

        float angleToPlayer = (float)Math.Atan2(dy, dx);
        float diff = MathHelpers.NormalizeAngle(angleToPlayer - facingAngle);
        if (Math.Abs(diff) > fovRadians / 2f) return false;

        int steps = Math.Max(4, (int)(dist / 6f));
        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            float sx = ex + dx * t;
            float sy = ey + dy * t;
            int tx = (int)(sx / TileSize);
            int ty = (int)(sy / TileSize);
            if (!InBounds(tx, ty) || world[tx, ty] == '#')
                return false;
        }

        return true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        g.Clear(Color.Black);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

        Render3D(g);
        RenderEnemies(g);
        RenderMiniMap(g);
        RenderHud(g);

        if (gameOver)
            RenderGameOver(g);
    }

    private void Render3D(Graphics g)
    {
        int width = ClientSize.Width;
        int height = ClientSize.Height;

        using SolidBrush skyBrush = new(Color.FromArgb(32, 34, 46));
        using SolidBrush floorBrush = new(Color.FromArgb(26, 24, 22));
        g.FillRectangle(skyBrush, 0, 0, width, height / 2);
        g.FillRectangle(floorBrush, 0, height / 2, width, height / 2);

        for (int x = 0; x < width; x++)
        {
            float rayAngle = (player.Angle - Fov / 2f) + ((float)x / width) * Fov;
            float eyeX = (float)Math.Cos(rayAngle);
            float eyeY = (float)Math.Sin(rayAngle);

            float distanceToWall = 0f;
            bool hitWall = false;
            bool boundary = false;

            while (!hitWall && distanceToWall < Depth)
            {
                distanceToWall += 2f;
                int testX = (int)((player.X + eyeX * distanceToWall) / TileSize);
                int testY = (int)((player.Y + eyeY * distanceToWall) / TileSize);

                if (!InBounds(testX, testY))
                {
                    hitWall = true;
                    distanceToWall = Depth;
                }
                else if (world[testX, testY] == '#')
                {
                    hitWall = true;
                    var corners = new List<(float d, float dot)>();
                    for (int tx = 0; tx < 2; tx++)
                    {
                        for (int ty = 0; ty < 2; ty++)
                        {
                            float vx = (testX + tx) * TileSize - player.X;
                            float vy = (testY + ty) * TileSize - player.Y;
                            float vd = (float)Math.Sqrt(vx * vx + vy * vy);
                            if (vd > 0.001f)
                            {
                                float dot = (eyeX * vx / vd) + (eyeY * vy / vd);
                                corners.Add((vd, dot));
                            }
                        }
                    }

                    corners = corners.OrderBy(v => v.d).ToList();
                    if (corners.Count >= 2)
                    {
                        float bound = 0.015f;
                        if ((float)Math.Acos(Math.Clamp(corners[0].dot, -1f, 1f)) < bound ||
                            (float)Math.Acos(Math.Clamp(corners[1].dot, -1f, 1f)) < bound)
                            boundary = true;
                    }
                }
            }

            float correctedDistance = distanceToWall * (float)Math.Cos(rayAngle - player.Angle);
            correctedDistance = Math.Max(1f, correctedDistance);
            depthBuffer[x] = correctedDistance;

            int ceiling = (int)(height / 2f - 22000f / correctedDistance);
            int floor = height - ceiling;
            ceiling = Math.Max(0, ceiling);
            floor = Math.Min(height - 1, floor);

            Color wallColor = RenderHelpers.GetWallColor(correctedDistance, boundary);
            using Pen wallPen = new(wallColor);
            if (floor > ceiling)
                g.DrawLine(wallPen, x, ceiling, x, floor);

            for (int y = floor + 1; y < height; y++)
            {
                float b = 1f - ((float)(y - height / 2) / (height / 2));
                Color floorColor = RenderHelpers.GetFloorColor(b);
                using Pen floorPen = new(floorColor);
                g.DrawLine(floorPen, x, y, x, y);
            }
        }
    }

    private void RenderEnemies(Graphics g)
    {
        int width = ClientSize.Width;
        int height = ClientSize.Height;

        foreach (Enemy enemy in enemies.OrderByDescending(e => MathHelpers.Distance(player.X, player.Y, e.X, e.Y)))
        {
            float vx = enemy.X - player.X;
            float vy = enemy.Y - player.Y;
            float distance = (float)Math.Sqrt(vx * vx + vy * vy);
            float angle = (float)Math.Atan2(vy, vx) - player.Angle;

            while (angle < -(float)Math.PI) angle += (float)(Math.PI * 2);
            while (angle > (float)Math.PI) angle -= (float)(Math.PI * 2);

            if (Math.Abs(angle) > Fov / 2f + 0.2f || distance < 8f || distance >= Depth)
                continue;

            int bodyHeight = (int)(24000f / distance);
            int bodyWidth = Math.Max(18, bodyHeight / 2);
            int screenX = (int)(((angle + Fov / 2f) / Fov) * width);
            int top = height / 2 - bodyHeight / 2;
            int left = screenX - bodyWidth / 2;

            bool visible = false;
            for (int sx = 0; sx < bodyWidth; sx++)
            {
                int column = left + sx;
                if (column < 0 || column >= width) continue;
                if (depthBuffer[column] >= distance)
                {
                    visible = true;
                    break;
                }
            }

            if (!visible)
                continue;

            RenderHelpers.RenderEnemyBody(g, enemy, left, top, bodyWidth, bodyHeight, hudFont);
        }
    }

    private void RenderMiniMap(Graphics g)
    {
        int offsetX = 16;
        int offsetY = 16;
        int miniW = mapWidth * MiniMapScale;
        int miniH = mapHeight * MiniMapScale;

        using SolidBrush back = new(Color.FromArgb(170, 10, 10, 14));
        using Pen border = new(Color.FromArgb(110, 160, 160, 180), 2f);
        g.FillRectangle(back, offsetX - 6, offsetY - 6, miniW + 12, miniH + 12);
        g.DrawRectangle(border, offsetX - 6, offsetY - 6, miniW + 12, miniH + 12);

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                Color c = world[x, y] == '#' ? Color.Gray : Color.FromArgb(42, 44, 48);
                using SolidBrush b = new(c);
                g.FillRectangle(b, offsetX + x * MiniMapScale, offsetY + y * MiniMapScale, MiniMapScale, MiniMapScale);
            }
        }

        foreach (Enemy enemy in enemies)
            DrawEnemyVisionOnMiniMap(g, enemy, offsetX, offsetY);

        float playerMiniX = offsetX + (player.X / TileSize) * MiniMapScale;
        float playerMiniY = offsetY + (player.Y / TileSize) * MiniMapScale;
        using SolidBrush pBrush = new(Color.DeepSkyBlue);
        g.FillEllipse(pBrush, playerMiniX - 4, playerMiniY - 4, 8, 8);
        using Pen facingPen = new(Color.White, 2f);
        g.DrawLine(facingPen, playerMiniX, playerMiniY,
            playerMiniX + (float)Math.Cos(player.Angle) * 14f,
            playerMiniY + (float)Math.Sin(player.Angle) * 14f);

        foreach (Enemy enemy in enemies)
        {
            float ex = offsetX + (enemy.X / TileSize) * MiniMapScale;
            float ey = offsetY + (enemy.Y / TileSize) * MiniMapScale;
            using SolidBrush eBrush = new(enemy.Alerted ? Color.OrangeRed : enemy.Color);
            g.FillEllipse(eBrush, ex - 4, ey - 4, 8, 8);
        }
    }

    private void DrawEnemyVisionOnMiniMap(Graphics g, Enemy enemy, int offsetX, int offsetY)
    {
        List<PointF> points = new();
        float ex = offsetX + (enemy.X / TileSize) * MiniMapScale;
        float ey = offsetY + (enemy.Y / TileSize) * MiniMapScale;
        points.Add(new PointF(ex, ey));

        int rays = 18;
        float halfFov = enemy.FovRadians / 2f;
        for (int i = 0; i <= rays; i++)
        {
            float angle = enemy.Angle - halfFov + (enemy.FovRadians * i / rays);
            PointF hit = CastVisionRay(enemy.X, enemy.Y, angle, enemy.SightDistance);
            points.Add(new PointF(
                offsetX + (hit.X / TileSize) * MiniMapScale,
                offsetY + (hit.Y / TileSize) * MiniMapScale));
        }

        using SolidBrush brush = new(enemy.Alerted ? Color.FromArgb(90, 255, 70, 70) : Color.FromArgb(75, 255, 220, 100));
        if (points.Count >= 3)
            g.FillPolygon(brush, points.ToArray());
    }

    private PointF CastVisionRay(float startX, float startY, float angle, float maxDistance)
    {
        const float step = 4f;
        for (float d = 0; d <= maxDistance; d += step)
        {
            float x = startX + (float)Math.Cos(angle) * d;
            float y = startY + (float)Math.Sin(angle) * d;
            int tx = (int)(x / TileSize);
            int ty = (int)(y / TileSize);
            if (!InBounds(tx, ty) || world[tx, ty] == '#')
                return new PointF(x, y);
        }

        return new PointF(
            startX + (float)Math.Cos(angle) * maxDistance,
            startY + (float)Math.Sin(angle) * maxDistance);
    }

    private void RenderHud(Graphics g)
    {
        int panelW = 320;
        int panelH = 210;
        int panelX = ClientSize.Width - panelW - 16;
        int panelY = 16;

        using SolidBrush panel = new(Color.FromArgb(175, 12, 12, 16));
        using Pen border = new(Color.FromArgb(100, 150, 150, 170), 2f);
        g.FillRectangle(panel, panelX, panelY, panelW, panelH);
        g.DrawRectangle(border, panelX, panelY, panelW, panelH);

        g.DrawString("MULL 3D", titleFont, Brushes.White, panelX + 16, panelY + 12);
        g.DrawString($"Time: {elapsedSeconds:0.0}s", hudFont, Brushes.Gainsboro, panelX + 16, panelY + 54);
        g.DrawString("W/S move  A/D strafe", hudFont, Brushes.LightSteelBlue, panelX + 16, panelY + 82);
g.DrawString("Mouse look  ESC quit", hudFont, Brushes.LightSteelBlue, panelX + 16, panelY + 102);        g.DrawString("Enemies", hudFont, Brushes.White, panelX + 16, panelY + 132);

        int y = panelY + 156;
        foreach (Enemy enemy in enemies)
        {
            string state = enemy.Alerted ? "ALERT" : "idle";
            using SolidBrush stateBrush = new(enemy.Alerted ? Color.OrangeRed : Color.Khaki);
            g.DrawString($"{enemy.Symbol} {enemy.Name}  {state}", hudFont, stateBrush, panelX + 16, y);
            y += 18;
        }
    }

    private void RenderGameOver(Graphics g)
    {
        using SolidBrush overlay = new(Color.FromArgb(120, 0, 0, 0));
        g.FillRectangle(overlay, 0, 0, ClientSize.Width, ClientSize.Height);

        Rectangle box = new(ClientSize.Width / 2 - 220, ClientSize.Height / 2 - 75, 440, 150);
        using SolidBrush boxBrush = new(Color.FromArgb(220, 20, 20, 28));
        using Pen boxPen = new(Color.OrangeRed, 3f);
        g.FillRectangle(boxBrush, box);
        g.DrawRectangle(boxPen, box);

        g.DrawString("GAME OVER", titleFont, Brushes.White, box.X + 120, box.Y + 22);
        g.DrawString(gameOverReason, hudFont, Brushes.Gainsboro, box.X + 92, box.Y + 66);
        g.DrawString("Press ESC to close", hudFont, Brushes.Khaki, box.X + 118, box.Y + 96);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        pressedKeys.Add(e.KeyCode);
        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        pressedKeys.Remove(e.KeyCode);
        base.OnKeyUp(e);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            Close();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);

        if (keyData == Keys.Tab)
{
    mouseLookEnabled = !mouseLookEnabled;

    if (mouseLookEnabled)
    {
        Cursor.Hide();
        UpdateMouseCenter();
        CenterMouse();
    }
    else
    {
        Cursor.Show();
    }

    return true;
}
    }
}