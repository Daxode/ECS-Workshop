// #define DEBUG_DRAW_CAVE_GRID
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Debug = UnityEngine.Debug;

struct MarchingSquareTileCarverTag : IComponentData {}
struct MarchingSquareTileMatATag : IComponentData {}
struct MarchingSquareTileMatBTag : IComponentData {}

[InternalBufferCapacity(1)]
struct MarchSquareSet : IBufferElementData
{
    // Left Top, Right Top, Right Bottom, Left Bottom
    public float2 offset0; // empty
    public float2 offset1; // 0001 - corner: LB
    public float2 offset2; // 0010 - corner: RB
    public float2 offset3; // 0011 - flat: bottom
    public float2 offset4; // 0100 - corner: RT
    public float2 offset5; // 0101 - diagonal: LB-RT
    public float2 offset6; // 0110 - flat: right
    public float2 offset7; // 0111 - curve: RT-RB-LB
    public float2 offset8; // 1000 - corner: LT
    public float2 offset9; // 1001 - flat: left
    public float2 offsetA; // 1010 - diagonal: LT-RB
    public float2 offsetB; // 1011 - curve: LT-RB-LB
    public float2 offsetC; // 1100 - flat: top
    public float2 offsetD; // 1101 - curve: LT-RT-LB
    public float2 offsetE; // 1110 - curve: LT-RT-RB
    public float2 offsetF; // full

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe float2 GetOffset(int val) 
        => UnsafeUtility.ReadArrayElement<float2>(UnsafeUtility.AddressOf(ref offset0), val);
}

struct MarchSquareData : IComponentData
{
    public Entity spriteTargetPrefab;
    public Entity gridLockPrefab;
}

public enum CaveMaterialType : byte
{
    Rock,
    Air,
    Ore,
    Water,
}

struct GridList : IDisposable
{
    NativeList<CaveMaterialType> m_Grid;
    public static implicit operator GridList(NativeList<CaveMaterialType> val) => new() { m_Grid = val };
    public const int Width = 20;
    public int GetHeight() => m_Grid.Length / Width;
    public void Dispose() => m_Grid.Dispose();
    public GridArray AsArray() => m_Grid.AsArray();
    public NativeList<CaveMaterialType> GetRawUnsafe() => m_Grid;
    public int Length => m_Grid.Length;
}

public struct GridArray
{
    NativeArray<CaveMaterialType> m_Grid;
    public static implicit operator GridArray(NativeArray<CaveMaterialType> val) => new() { m_Grid = val };
    public const int Width = 20;
    public int GetHeight() => m_Grid.Length / Width;
    public NativeArray<CaveMaterialType> GetRawUnsafe() => m_Grid;
    public int Length => m_Grid.Length;
    public CaveMaterialType this[IndexFor<GridArray> i]
    {
        get => m_Grid[i];
        set => m_Grid[i] = value;
    }
}

struct TileList : IDisposable
{
    NativeList<Entity> m_Tiles;
    public static implicit operator TileList(NativeList<Entity> val) => new() { m_Tiles = val };
    public const int Width = GridList.Width-1;
    public int GetCaveTileHeight() => m_Tiles.Length / Width;
    public void Dispose() => m_Tiles.Dispose();
    public TileArray AsArray() => m_Tiles.AsArray();
    public NativeList<Entity> GetRawUnsafe() => m_Tiles;
    public int Length => m_Tiles.Length;
}

public struct TileArray
{
    NativeArray<Entity> m_Tiles;
    public static implicit operator TileArray(NativeArray<Entity> val) => new() { m_Tiles = val };
    public const int Width = GridList.Width-1;
    public int GetCaveTileHeight() => m_Tiles.Length / Width;
    public NativeArray<Entity> GetRawUnsafe() => m_Tiles;
    public int Length => m_Tiles.Length;
    
    public Entity this[IndexFor<TileArray> i]
    {
        get => m_Tiles[i];
        set => m_Tiles[i] = value;
    }
}

public static class CoordUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IndexFor<TileArray> WorldPosToTileIndex(float2 worldPos) 
        => new Int2For<TileArray>((int2)math.round(worldPos)).GetIndex();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2For<GridArray> WorldPosToGridPos(float2 worldPos) 
        => new((int2)math.round(worldPos + new float2(0.5f, -0.5f)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2For<GridArray> LocalToWorldToGridPos(LocalToWorld ltw) 
        => new((int2)ltw.Position.xy);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float2 SnapWorldPosToGridPos(float2 worldPos) =>
        (Float2For<GridArray>)WorldPosToGridPos(worldPos) - new float2(0.5f, -0.5f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IndexFor<GridArray> WorldPosToGridIndex(float2 worldPos) => WorldPosToGridPos(worldPos).GetIndex();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (float2 pos, bool snappedToTile) SnapToTileOrGrid(float2 worldPos)
    {
        var gridPos = SnapWorldPosToGridPos(worldPos);
        var tilePos = math.round(worldPos);
        var snappedToTile = math.distancesq(worldPos, tilePos) < math.distancesq(worldPos, gridPos);
        return (math.select(gridPos, tilePos, snappedToTile), snappedToTile);
    }


    
    // GridArray Helpers
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IndexFor<GridArray> GetIndex(this Int2For<GridArray> gridPos)
    {
        AssertBounds(gridPos);
        return new IndexFor<GridArray>(gridPos.X + (-gridPos.Y * GridArray.Width));
    }
    [BurstDiscard]
    [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void AssertBounds(Int2For<GridArray> gridPos) 
        => Debug.Assert(gridPos.X is >= 0 and < GridArray.Width && gridPos.Y <= 0,
            $"Grid position {(int2)gridPos} out of bounds (remember that Y has to be negative)");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IndexFor<GridArray> GoRight(this IndexFor<GridArray> gridIndex)
        => new (gridIndex + 1);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IndexFor<GridArray> GoDown(this IndexFor<GridArray> gridIndex)
        => new (gridIndex + GridArray.Width);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int4 GetCornerValues(this GridArray caveGrid, Int2For<GridArray> coord)
    {
        var i = coord.GetIndex();
        return new int4(
            (int)caveGrid[i.GoDown()], // Bottom Left
            (int)caveGrid[i.GoDown().GoRight()], // Bottom Right
            (int)caveGrid[i.GoRight()], // Top Right
            (int)caveGrid[i] // Top Left
        );
    }
    
    
    // TileArray Helpers
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IndexFor<TileArray> GetIndex(this Int2For<TileArray> tilePos)
    {
        AssertBounds(tilePos);
        return new IndexFor<TileArray>(tilePos.X + (-tilePos.Y * TileArray.Width));
    }
    [BurstDiscard]
    [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void AssertBounds(Int2For<TileArray> tilePos) 
        => Debug.Assert(tilePos.X is >= 0 and < TileArray.Width && tilePos.Y <= 0, 
            $"Tile position {(int2)tilePos} out of bounds (remember that Y has to be negative)");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IndexFor<GridArray> GoRight(this IndexFor<TileArray> tilePos)
        => new (tilePos + 1);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IndexFor<GridArray> GoDown(this IndexFor<TileArray> tilePos)
        => new (tilePos + TileArray.Width);
    
}

[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
[UpdateAfter(typeof(TransformSystemGroup))]
public partial struct CaveGridSystem : ISystem, ISystemStartStop
{
    public struct Singleton : IComponentData, IDisposable
    {
        GridList m_CaveGrid;
        TileList m_CaveTiles;
        
        public const int DefaultInitRowCount = 128;
        
        public Singleton(Allocator allocator)
        {
            var caveGrid = new NativeList<CaveMaterialType>(GridList.Width * DefaultInitRowCount, allocator);
            var caveTiles = new NativeList<Entity>(TileList.Width * DefaultInitRowCount, allocator);
            m_CaveGrid = caveGrid;
            m_CaveTiles = caveTiles;
        }

        public TileArray TileArray => m_CaveTiles.AsArray();
        public GridArray GridArray => m_CaveGrid.AsArray();
        
        public int UnsafeGridLength
        {
            get => m_CaveGrid.GetRawUnsafe().Length;
            set
            {
                var raw = m_CaveGrid.GetRawUnsafe();
                raw.Length = value;
            }
        }
        
        public int UnsafeTileLength
        {
            get => m_CaveTiles.GetRawUnsafe().Length;
            set
            {
                var raw = m_CaveTiles.GetRawUnsafe();
                raw.Length = value;
            }
        }
        
        public void UnsafeResizeGrid(int length, NativeArrayOptions options) 
            => m_CaveGrid.GetRawUnsafe().Resize(length, options);
        public void UnsafeResizeTile(int length, NativeArrayOptions options)
            => m_CaveTiles.GetRawUnsafe().Resize(length, options);

        public void Dispose()
        {
            m_CaveGrid.Dispose();
            m_CaveTiles.Dispose();
        }
    }

    // Generate the cave grid
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var singleton = new Singleton(Allocator.Persistent);
        state.EntityManager.AddComponentData(state.SystemHandle, singleton);
        singleton.UnsafeGridLength = GridList.Width * Singleton.DefaultInitRowCount;
        singleton.UnsafeResizeTile(TileList.Width * (Singleton.DefaultInitRowCount-1), NativeArrayOptions.ClearMemory);
        var gridArray = singleton.GridArray;
        for (var i = new IndexFor<GridArray>(0); i < singleton.GridArray.Length; i++)
        {
            var x = i % GridList.Width;
            var y = i / GridList.Width;
            
            // generate cave grid
            var water = math.select(0, (int)CaveMaterialType.Water, noise.cnoise(new float2(x, -y)*0.1f)>0.4f);     // water
            var ore = math.select(0, (int)CaveMaterialType.Ore, noise.cnoise(new float3(x, -y, 0.3f)*0.1f)>0.2f); // ore
            var air = math.select(0, (int)CaveMaterialType.Air, noise.cnoise(new float3(x, -y, 0.6f)*0.1f)>0.3f); // air
            air = math.max(air, y is 0 or 1 && x is 6 or 7 ? (int)CaveMaterialType.Air : (int)CaveMaterialType.Rock);       // cave entrance
            gridArray[i] = (CaveMaterialType) math.max(math.max(water, ore), air);

#if DEBUG_DRAW_CAVE_GRID
            // Debug draw the cave grid
            Debug.DrawLine(
                new float3(x, -y, 0), 
                new float3(x, -y, 0) + math.forward(), 
                Color.HSVToRGB((float)gridArray[i]/4f,1,1), 
                100f);
#endif
        }
        
        state.RequireForUpdate<MarchSquareData>();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) 
        => SystemAPI.GetComponent<Singleton>(state.SystemHandle).Dispose();

    // Spawn the tiles
    [BurstCompile]
    public void OnStartRunning(ref SystemState state)
    {
        var spriteTargetPrefab = SystemAPI.GetSingleton<MarchSquareData>().spriteTargetPrefab;
        var caveGridHeight = SystemAPI.GetComponent<Singleton>(state.SystemHandle).GridArray.GetHeight();
        
        for (var y = 0; y > -caveGridHeight + 1; y--)
        {
            for (var x = 0; x < GridList.Width - 1; x++)
            {
                // spawn a sprite with the correct offset
                var spriteTarget = state.EntityManager.Instantiate(spriteTargetPrefab);
                SystemAPI.SetComponent(spriteTarget, LocalTransform.FromPosition(new float3(x, y, 0)));
            }
        }
    }

    // Update the tiles
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var marchSquareSets = SystemAPI.GetSingletonBuffer<MarchSquareSet>();
        var caveGrid = SystemAPI.GetComponent<Singleton>(state.SystemHandle).GridArray;
        
        // update outline tile
        foreach (var (lt, offsetXYScaleZwRef) in SystemAPI.Query<LocalToWorld, RefRW<MaterialOverrideOffsetXYScaleZW>>().WithAll<MarchingSquareTileCarverTag>())
        {
            var corners = caveGrid.GetCornerValues(CoordUtility.LocalToWorldToGridPos(lt));

            // build a 4-bit value from the 4 corners
            corners = 1 - (int4) math.saturate(corners);
            var valCombined = corners.x | (corners.y << 1) | (corners.z << 2) | (corners.w << 3);
            offsetXYScaleZwRef.ValueRW.Value.xy = marchSquareSets[0].GetOffset(valCombined);
        }
        
        // update mat A tile (the one with the highest corner)
        foreach (var (lt, offsetXYScaleZwRef, cornerStrengthRef) in SystemAPI.Query<LocalToWorld, RefRW<MaterialOverrideOffsetXYScaleZW>, RefRW<MaterialOverrideCornerStrength>>().WithAll<MarchingSquareTileMatATag>())
        {
            var corners = caveGrid.GetCornerValues(CoordUtility.LocalToWorldToGridPos(lt));
            var highestCorners = SortTheFourNumbers(corners);
            
            // build a 4-bit value from the 4 corners
            corners = 1-(int4) (corners == highestCorners.w);
            var valCombined = corners.x | (corners.y << 1) | (corners.z << 2) | (corners.w << 3);
            offsetXYScaleZwRef.ValueRW.Value.xy = marchSquareSets[math.clamp(highestCorners.w-1, 0, marchSquareSets.Length-1)].GetOffset(valCombined);
        }
        
        // update mat B tile (same as mat A, but with the second highest corner)
        foreach (var (lt, offsetXYScaleZwRef, cornerStrengthRef) in SystemAPI.Query<LocalToWorld, RefRW<MaterialOverrideOffsetXYScaleZW>, RefRW<MaterialOverrideCornerStrength>>().WithAll<MarchingSquareTileMatBTag>())
        {
            var corners = caveGrid.GetCornerValues(CoordUtility.LocalToWorldToGridPos(lt));
            var highestCorners = SortTheFourNumbers(corners);
            
            // get the second highest corner
            var validSecond = 0;
            if (highestCorners.w != highestCorners.z)
                validSecond = highestCorners.z;
            else if (highestCorners.w != highestCorners.y)
                validSecond = highestCorners.y;
            else if (highestCorners.w != highestCorners.x)
                validSecond = highestCorners.x;
            
            // if there is no second highest corner, then there is no corner
            cornerStrengthRef.ValueRW.Value = validSecond == 0 ? 0 : 1;
            
            
            // build a 4-bit value from the 4 corners
            corners = 1-(int4) (corners == validSecond);
            var valCombined = corners.x | (corners.y << 1) | (corners.z << 2) | (corners.w << 3);
            offsetXYScaleZwRef.ValueRW.Value.xy = marchSquareSets[math.clamp(validSecond-1, 0, marchSquareSets.Length-1)].GetOffset(valCombined);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // Sort the 4 numbers from lowest to highest (lowest in x, highest in w)
    static int4 SortTheFourNumbers(int4 val)
    {
        var lowHigh1 = math.select(val.yx, val.xy, val.x < val.y);
        var lowHigh2 = math.select(val.wz, val.zw, val.z < val.w);
        var lowestMiddle1 = math.select(new int2(lowHigh2.x, lowHigh1.x), new int2(lowHigh1.x, lowHigh2.x), lowHigh1.x < lowHigh2.x);
        var middle2Highest = math.select(new int2(lowHigh2.y, lowHigh1.y), new int2(lowHigh1.y, lowHigh2.y), lowHigh1.y < lowHigh2.y);

        return math.select(
            new int4(lowestMiddle1.x,middle2Highest.x, lowestMiddle1.y, middle2Highest.y), 
            new int4(lowestMiddle1, middle2Highest), 
            lowestMiddle1.y < middle2Highest.x);
    }

    public void OnStopRunning(ref SystemState state) {}
}