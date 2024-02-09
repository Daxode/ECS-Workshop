using System.Runtime.CompilerServices;
using Unity.Mathematics;

public struct Float2For<T> where T : unmanaged
{
    float2 m_Value;
    
    public Float2For(float2 val) 
        => m_Value = val;
    public Float2For(float x, float y) 
        => m_Value = new float2(x, y);

    public float X {get => m_Value.x; set => m_Value.x = value;}
    public float Y {get => m_Value.y; set => m_Value.y = value;}
    
    public static implicit operator float2(Float2For<T> f) => f.m_Value;
    public static explicit operator Float2For<T>(float2 f) => new() { m_Value = f };
    public static implicit operator Int2For<T>(Float2For<T> f) => (Int2For<T>)(int2)f.m_Value;
}

public struct Int2For<T> where T : unmanaged
{
    int2 m_Value;

    public Int2For(int2 val) 
        => m_Value = val;

    public Int2For(int x, int y) 
        => m_Value = new int2(x, y);

    public int X {get => m_Value.x; set => m_Value.x = value;}
    public int Y {get => m_Value.y; set => m_Value.y = value;}
    
    public static implicit operator int2(Int2For<T> f) => f.m_Value;
    public static explicit operator Int2For<T>(int2 f) => new() { m_Value = f };
    public static explicit operator Float2For<T>(Int2For<T> f) => (Float2For<T>)(float2)f.m_Value;
}

public struct IndexFor<T> where T : unmanaged
{
    int m_Value;

    public IndexFor(int val) => m_Value = val;
    public static implicit operator int(IndexFor<T> f) => f.m_Value;
    public static explicit operator IndexFor<T>(int f) => new() { m_Value = f };
    
    // addition operator
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IndexFor<T> operator +(IndexFor<T> a, int b) => new (a.m_Value + b);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IndexFor<T> operator ++(IndexFor<T> a) => new (a.m_Value + 1);
}