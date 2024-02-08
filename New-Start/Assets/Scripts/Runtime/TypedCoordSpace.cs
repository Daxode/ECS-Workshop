using System;

/// <summary>
/// Wrapper to distinguish an integer node index in the grid from a raw integer.
/// </summary>
public struct GridNodeIndex : IComparable<GridNodeIndex>
{
    public int Index;
    public static implicit operator int(GridNodeIndex n) => n.Index;
    public static implicit operator GridNodeIndex(int i) => new GridNodeIndex { Index = i };
    public int CompareTo(GridNodeIndex other)
    {
        return Index.CompareTo(other.Index);
    }
}