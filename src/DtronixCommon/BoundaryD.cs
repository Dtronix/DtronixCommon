﻿using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DtronixCommon;

/// <summary>
/// BoundaryD represented as [MinX, MinY] (Bottom Left) and [MaxX, MaxY] (Top Right) points.
/// </summary>
/// <remarks>
/// MinY could be down and MaxY up as in cartesian coordinates, or vice vercia as is common
/// in screen coordinates.  Depends upon which system was selected for use.
/// </remarks>
public readonly struct BoundaryD
{
    /// <summary>
    /// Minimum X coordinate position. (Left)
    /// </summary>
    public readonly double MinX;

    /// <summary>
    /// Minimum Y coordinate position (Bottom/Top)
    /// </summary>
    public readonly double MinY;

    /// <summary>
    /// Maximum X coordinate position (Right)
    /// </summary>
    public readonly double MaxX;

    /// <summary>
    /// Maximum Y coordinate position (Top/Bottom)
    /// </summary>
    public readonly double MaxY;

    /// <summary>
    /// Width of the boundary.
    /// </summary>
    public readonly double Width => MaxX - MinX;

    /// <summary>
    /// Height of the boundary.
    /// </summary>
    public readonly double Height => MaxY - MinY;

    /// <summary>
    /// Maximum sized boundary.
    /// </summary>
    public static BoundaryD Max { get; } = new(
        double.MinValue,
        double.MinValue,
        double.MaxValue,
        double.MaxValue);

    /// <summary>
    /// Half the maximum size of the boundary.
    /// </summary>
    public static BoundaryD HalfMax { get; } = new(
        double.MinValue / 2,
        double.MinValue / 2,
        double.MaxValue / 2,
        double.MaxValue / 2);

    /// <summary>
    /// Zero sized boundary.
    /// </summary>
    public static BoundaryD Zero { get; } = new(0, 0, 0, 0);

    /// <summary>
    /// Empty boundary.
    /// </summary>
    public static BoundaryD Empty { get; } = new(
        double.PositiveInfinity,
        double.PositiveInfinity,
        double.NegativeInfinity,
        double.NegativeInfinity);

    /// <summary>
    /// Returns true if the boundary has no volume.
    /// </summary>
    public bool IsEmpty => MinY >= MaxY && MinX >= MaxX;

    /// <summary>
    /// Creates a boundary with the specified left, bottom, right, top distances from origin.
    /// </summary>
    /// <param name="minX">Left distance from origin.</param>
    /// <param name="minY">Bottom distance from origin.</param>
    /// <param name="maxX">Right distance from origin.</param>
    /// <param name="maxY">Top distance from origin.</param>
    public BoundaryD(double minX, double minY, double maxX, double maxY)
    {
        MinX = minX;
        MinY = minY;
        MaxX = maxX;
        MaxY = maxY;
    }

    /// <summary>
    /// IntersectsWith - Returns true if the BoundaryD intersects with this BoundaryD
    /// Returns false otherwise.
    /// Note that if one edge is coincident, this is considered an intersection.
    /// </summary>
    /// <returns>
    /// Returns true if the BoundaryD intersects with this BoundaryD
    /// Returns false otherwise.
    /// </returns>
    /// <param name="boundary"> Rect </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IntersectsWith(in BoundaryD boundary)
    {
        return boundary.MinX <= MaxX &&
               boundary.MaxX >= MinX &&
               boundary.MinY <= MaxY &&
               boundary.MaxY >= MinY;
    }

    /// <summary>
    /// IntersectsWith - Returns true if the BoundaryD intersects with this BoundaryD
    /// Returns false otherwise.
    /// Note that if one edge is coincident, this is considered an intersection.
    /// </summary>
    /// <param name="boundary1">First boundary.</param>
    /// <param name="boundary2">Second boundary.</param>
    /// <returns>
    /// Returns true if the BoundaryD intersects with this BoundaryD
    /// Returns false otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Intersects(in BoundaryD boundary1, in BoundaryD boundary2)
    {
        return boundary1.MinX <= boundary2.MaxX &&
               boundary1.MaxX >= boundary2.MinX &&
               boundary1.MinY <= boundary2.MaxY &&
               boundary1.MaxY >= boundary2.MinY;
    }

    /// <summary>
    /// Checks if a point is contained by the boundary.
    /// </summary>
    /// <param name="x">X coordinate.</param>
    /// <param name="y">Y coordinate.</param>
    /// <returns>True if the point is contained, false otherwise.</returns>
    public bool Contains(double x, double y)
    {
        return x >= MinX
               && x <= MaxX
               && y >= MinY
               && y <= MaxY;
    }


    /// <summary>
    /// Takes the current boundary and offsets it by the specified X & Y vectors.
    /// </summary>
    /// <param name="x">X vector offset.</param>
    /// <param name="y">Y vector offset.</param>
    /// <returns>New offset boundary.</returns>
    public BoundaryD CreateOffset(in double x, in double y)
    {
        return new BoundaryD(
            (double)(MinX + x),
            (double)(MinY + y),
            (double)(MaxX + x),
            (double)(MaxY + y));
    }

    /// <summary>
    /// Creates a union between two boundaries.
    /// </summary>
    /// <param name="boundary">second boundary to union with.</param>
    /// <returns>new union boundary.</returns>
    public BoundaryD Union(in BoundaryD boundary)
    {
        if (IsEmpty)
            return boundary;

        if (boundary.IsEmpty)
            return this;

        return new BoundaryD(
            Math.Min(MinX, boundary.MinX),
            Math.Min(MinY, boundary.MinY),
            Math.Max(MaxX, boundary.MaxX),
            Math.Max(MaxY, boundary.MaxY));
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
    public double GetHypotenuseApproximate(int algorithm)
    {
        var a = Math.Abs(MaxX - MinX);
        var b = Math.Abs(MaxY - MinY);

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
                return Math.Max(b, 0.918f * (b + (a / 2f)));
            case 3:
                return b + 0.428f * a * a / b;
            default:
                throw new ArgumentException("Must select algorithm 0 through 3", nameof(algorithm));
        }

    }

    /// <summary>
    /// Rotates the boundary from its center point by the specified number of degrees.
    /// </summary>
    /// <param name="degrees">Degrees to rotate.</param>
    /// <returns>New rotated boundary.</returns>
    public BoundaryD Rotate(double degrees)
    {
        // Center position of the rectangle.
        //private const m_PosX : Number, m_PosY : Number;

        // Rectangle orientation, in radians.
        var m_Orientation = degrees * Math.PI / 180.0f;
        // Half-width and half-height of the rectangle.
        var m_HalfSizeX = Width / 2;
        var m_HalfSizeY = Height / 2;
        var m_PosX = MinX + m_HalfSizeX;
        var m_PosY = MinY + m_HalfSizeY;

        // corner_1 is right-top corner of unrotated rectangle, relative to m_Pos.
        // corner_2 is right-bottom corner of unrotated rectangle, relative to m_Pos.
        var corner_1_x = m_HalfSizeX;
        var corner_2_x = m_HalfSizeX;
        var corner_1_y = -m_HalfSizeY;
        var corner_2_y = m_HalfSizeY;

        var sin_o = Math.Sin(m_Orientation);
        var cos_o = Math.Cos(m_Orientation);

        // xformed_corner_1, xformed_corner_2 are points corner_1, corner_2 rotated by angle m_Orientation.
        var xformed_corner_1_x = corner_1_x * cos_o - corner_1_y * sin_o;
        var xformed_corner_1_y = corner_1_x * sin_o + corner_1_y * cos_o;
        var xformed_corner_2_x = corner_2_x * cos_o - corner_2_y * sin_o;
        var xformed_corner_2_y = corner_2_x * sin_o + corner_2_y * cos_o;

        // ex, ey are extents (half-sizes) of the final AABB.
        var ex = Math.Max(Math.Abs(xformed_corner_1_x), Math.Abs(xformed_corner_2_x));
        var ey = Math.Max(Math.Abs(xformed_corner_1_y), Math.Abs(xformed_corner_2_y));
        return new BoundaryD(m_PosX - ex, m_PosY - ey, m_PosX + ex, m_PosY + ey);
        //var aabb_min_x = m_PosX - ex;
        //var aabb_max_x = m_PosX + ex;
        //var aabb_min_y = m_PosY - ey;
        //var aabb_max_y = m_PosY + ey;
    }

    /// <summary>
    /// Unions two boundaries.
    /// </summary>
    /// <param name="boundary1">First boundary.</param>
    /// <param name="boundary2">Second boundary.</param>
    /// <returns>New union boundary.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BoundaryD Union(in BoundaryD boundary1, in BoundaryD boundary2)
    {
        return new BoundaryD(
            boundary1.MinX < boundary2.MinX ? boundary1.MinX : boundary2.MinX,
            boundary1.MinY < boundary2.MinY ? boundary1.MinY : boundary2.MinY,
            boundary1.MaxX > boundary2.MaxX ? boundary1.MaxX : boundary2.MaxX,
            boundary1.MaxY > boundary2.MaxY ? boundary1.MaxY : boundary2.MaxY);
    }

    /// <summary>
    /// Returns a String which represents the boundary instance.
    /// </summary>
    /// <returns>Value</returns>
    public override string ToString()
    {
        return $"MinX:{MinX:F}; MinY:{MinY:F}; MaxX:{MaxX:F}; MaxY:{MaxY:F}; Width: {MaxX - MinX:F}; Height: {MaxY - MinY:F}";
    }


    /// <summary>
    /// Checks to see if the the two boundaries are equal.
    /// </summary>
    /// <param name="boundary1">First boundary.</param>
    /// <param name="boundary2">Second boundary.</param>
    /// <returns>True if the two boundaries are equal.</returns>
    public static bool operator ==(in BoundaryD boundary1, in BoundaryD boundary2)
    {
        return boundary1.Equals(boundary2);
    }

    /// <summary>
    /// Checks to see if the the two boundaries are not equal.
    /// </summary>
    /// <param name="boundary1">First boundary.</param>
    /// <param name="boundary2">Second boundary.</param>
    /// <returns>True if the two boundaries are not equal.</returns>
    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
    public static bool operator !=(in BoundaryD boundary1, in BoundaryD boundary2)
    {
        return boundary1.MaxX != boundary2.MaxX ||
               boundary1.MinX != boundary2.MinX ||
               boundary1.MaxY != boundary2.MaxY ||
               boundary1.MinY != boundary2.MinY;
    }

    /// <summary>
    /// Checks to see if the other boundary is equal to this boundary.
    /// </summary>
    /// <param name="other">Other boundary.</param>
    /// <returns>True if the two boundaries are equal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(in BoundaryD other)
    {
        return MinX.Equals(other.MinX)
               && MinY.Equals(other.MinY)
               && MaxX.Equals(other.MaxX)
               && MaxY.Equals(other.MaxY);
    }


    /// <summary>
    /// Checks to see if the other object is a boundary and if it is, if it is equal to this boundary.
    /// </summary>
    /// <param name="other">Other boundary.</param>
    /// <returns>True if the two boundaries are equal.</returns>
    public override bool Equals(object? obj)
    {
        return obj is BoundaryD other && Equals(other);
    }

    /// <summary>
    /// Gets the hash code of this boundary.
    /// </summary>
    /// <returns>Hash.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(MinX, MinY, MaxX, MaxY);
    }

    /// <summary>
    /// Creates a new boundary from a circle at the specified location.
    /// </summary>
    /// <param name="x">X coordinate of the circle center.</param>
    /// <param name="y">Y coordinate of the circle center.</param>
    /// <param name="radius">Radius of the circle.</param>
    /// <returns>New boundary.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BoundaryD FromCircle(double x, double y, double radius)
    {
        return new BoundaryD(
            x - radius,
            y - radius,
            x + radius,
            y + radius);
    }
}
