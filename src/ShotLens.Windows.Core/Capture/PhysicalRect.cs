namespace ShotLens.Windows.Core.Capture;

public readonly record struct PhysicalRect(int X, int Y, int Width, int Height)
{
    public int Right => checked(X + Width);

    public int Bottom => checked(Y + Height);
}
