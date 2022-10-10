using System.Runtime.CompilerServices;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace DtronixCommon.Structures;

/// <summary>
/// Boundary represented as [MinX, MinY] (Bottom Left) and [MaxX, MaxY] (Top Right) points.
/// </summary>
public readonly struct Boundary
{
    /// <summary>
    /// Minimum X coordinate position. (Left)
    /// </summary>
    public readonly float MinX;

    /// <summary>
    /// Minimum Y coordinate position (Bottom)
    /// </summary>
    public readonly float MinY;

    /// <summary>
    /// Maximum X coordinate position (Right)
    /// </summary>
    public readonly float MaxX;

    /// <summary>
    /// Maximum Y coordinate position (Top)
    /// </summary>
    public readonly float MaxY;

    public float Width => MaxX - MinX;
    public float Height => MaxY - MinY;

    /// <summary>
    /// Maximum sized viewport.
    /// </summary>
    public static Boundary Max { get; } = new(
        float.MinValue,
        float.MinValue,
        float.MaxValue,
        float.MaxValue);

    public static Boundary HalfMax { get; } = new(
        float.MinValue / 2,
        float.MinValue / 2,
        float.MaxValue / 2,
        float.MaxValue / 2);

    public static Boundary Zero { get; } = new(0, 0, 0, 0);

    /// <summary>
    /// Empty viewport.
    /// </summary>
    public static Boundary Empty { get; } = new(
        float.PositiveInfinity,
        float.PositiveInfinity,
        float.NegativeInfinity,
        float.NegativeInfinity);

    /// <summary>
    /// Returns true if the viewport has no volume.
    /// </summary>
    public bool IsEmpty => MinY >= MaxY || MinX >= MaxX;
    
    /// <summary>
    /// Creates a viewport with the specified left, bottom, right, top distances from origin.
    /// </summary>
    /// <param name="minX">Left distance from origin.</param>
    /// <param name="minY">Bottom distance from origin.</param>
    /// <param name="maxX">Right distance from origin.</param>
    /// <param name="maxY">Top distance from origin.</param>
    public Boundary(float minX, float minY, float maxX, float maxY)
    {
        MinX = minX;
        MinY = minY;
        MaxX = maxX;
        MaxY = maxY;
    }

    /// <summary>
    /// IntersectsWith - Returns true if the Boundary intersects with this Boundary
    /// Returns false otherwise.
    /// Note that if one edge is coincident, this is considered an intersection.
    /// </summary>
    /// <returns>
    /// Returns true if the Boundary intersects with this Boundary
    /// Returns false otherwise.
    /// </returns>
    /// <param name="viewport"> Rect </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IntersectsWith(in Boundary viewport)
    {
        return viewport.MinX <= MaxX &&
               viewport.MaxX >= MinX &&
               viewport.MinY <= MaxY &&
               viewport.MaxY >= MinY;
    }

    /// <summary>
    /// IntersectsWith - Returns true if the Boundary intersects with this Boundary
    /// Returns false otherwise.
    /// Note that if one edge is coincident, this is considered an intersection.
    /// </summary>
    /// <returns>
    /// Returns true if the Boundary intersects with this Boundary
    /// Returns false otherwise.
    /// </returns>
    /// <param name="viewport"> Rect </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Intersects(in Boundary viewport1, in Boundary viewport2)
    {
        return viewport1.MinX <= viewport2.MaxX &&
               viewport1.MaxX >= viewport2.MinX &&
               viewport1.MinY <= viewport2.MaxY &&
               viewport1.MaxY >= viewport2.MinY;
    }

    /// <summary>
    /// Gets a rough approximation of the hypotenuse of the rectangle.
    /// Uses different algorithms based upon the passed integer.
    /// </summary>
    /// <param name="algorithm">0 - 3. Determines algorithm used.  0 is the fastest, 3 is the slowest but most accurate.</param>
    /// <returns>Approximate length</returns>
    /// <remarks>
    /// https://stackoverflow.com/a/26607206
    /// All these assume 0 ≤ a ≤ b.
    /// 0. h = b + 0.337 * a                   // less sorting order of the a & b variables;
    /// 1. h = b + 0.337 * a                   // max error ≈ 5.5 %
    /// 2. h = max(b, 0.918 * (b + (a >> 1)))  // max error ≈ 2.6 %
    /// 3. h = b + 0.428 * a* a / b            // max error ≈ 1.04 %
    /// </remarks>
    public float GetHypotenuseApproximate(int algorithm)
    {
        var a = MathF.Abs(MaxX - MinX);
        var b = MathF.Abs(MaxY - MinY);

        if (algorithm == 0)
            return b + 0.337f * a;

        // Transpose variables to ensure "b" is larger than A
        if (a > b)
            (a, b) = (b, a);

        switch (algorithm)
        {
            case 1:
                return b + 0.337f * a;
            case 2:
                return MathF.Max(b, 0.918f * (b + (a / 2f)));
            case 3:
                return b + 0.428f * a * a / b;
            default:
                throw new ArgumentException("Must select algorithm 0 through 3", nameof(algorithm));
        }

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Boundary Union(in Boundary viewport1, in Boundary viewport2)
    {
        return new Boundary(
            viewport1.MinX < viewport2.MinX ? viewport1.MinX : viewport2.MinX,
            viewport1.MinY < viewport2.MinY ? viewport1.MinY : viewport2.MinY,
            viewport1.MaxX > viewport2.MaxX ? viewport1.MaxX : viewport2.MaxX,
            viewport1.MaxY > viewport2.MaxY ? viewport1.MaxY : viewport2.MaxY);
    }
    
    public override string ToString()
    {
        return $"MinX:{MinX:F}; MinY:{MinY:F}; MaxX:{MaxX:F}; MaxY:{MaxY:F}; Width: {Width:F}; Height: {Height:F}";
    }

    public static bool operator ==(in Boundary rect1, in Boundary rect2)
    {
        return rect1.Equals(rect2);
    }

    public static bool operator !=(in Boundary rect1, in Boundary rect2)
    {
        return rect1.MinX != rect2.MaxX ||
               rect1.MaxX != rect2.MinX ||
               rect1.MinY != rect2.MaxY ||
               rect1.MaxY != rect2.MinY;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(in Boundary other)
    {
        return MinX.Equals(other.MinX)
               && MinY.Equals(other.MinY)
               && MaxX.Equals(other.MaxX)
               && MaxY.Equals(other.MaxY);
    }

    public override bool Equals(object? obj)
    {
        return obj is Boundary other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(MinX, MinY, MaxX, MaxY);
    }
}
