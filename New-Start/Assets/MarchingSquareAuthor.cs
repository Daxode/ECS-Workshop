// #define DEBUG_DRAW_CAVE_GRID
using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

class MarchingSquareAuthor : MonoBehaviour
{
    public MarchSquareSetSprites[] sets;
    public MarchingSquareTile spriteTargetPrefab;
}

[Serializable]
class MarchSquareSetSprites
{
    public Sprite[] sprites = new Sprite[16];
}

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
}

class MarchingSquareBaker : Baker<MarchingSquareAuthor>
{
    public override void Bake(MarchingSquareAuthor authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new MarchSquareData
        {
            spriteTargetPrefab = GetEntity(authoring.spriteTargetPrefab, TransformUsageFlags.Renderable),
        });
        var buffer = AddBuffer<MarchSquareSet>(entity);

        var textureSheetTexelSize = authoring.spriteTargetPrefab.spriteTextureSheet.texelSize;
        
        foreach (var set in authoring.sets)
        {
            if (set.sprites.Length != 16)
                throw new Exception("MarchingSquareAuthor: set must have 16 sprites");
            
            buffer.Add(new MarchSquareSet
            {
                offset0 = set.sprites[0].rect.position * textureSheetTexelSize,
                offset1 = set.sprites[1].rect.position * textureSheetTexelSize,
                offset2 = set.sprites[2].rect.position * textureSheetTexelSize,
                offset3 = set.sprites[3].rect.position * textureSheetTexelSize,
                offset4 = set.sprites[4].rect.position * textureSheetTexelSize,
                offset5 = set.sprites[5].rect.position * textureSheetTexelSize,
                offset6 = set.sprites[6].rect.position * textureSheetTexelSize,
                offset7 = set.sprites[7].rect.position * textureSheetTexelSize,
                offset8 = set.sprites[8].rect.position * textureSheetTexelSize,
                offset9 = set.sprites[9].rect.position * textureSheetTexelSize,
                offsetA = set.sprites[10].rect.position * textureSheetTexelSize,
                offsetB = set.sprites[11].rect.position * textureSheetTexelSize,
                offsetC = set.sprites[12].rect.position * textureSheetTexelSize,
                offsetD = set.sprites[13].rect.position * textureSheetTexelSize,
                offsetE = set.sprites[14].rect.position * textureSheetTexelSize,
                offsetF = set.sprites[15].rect.position * textureSheetTexelSize,
            });
        }
    }
}

[UpdateBefore(typeof(CaveGridSystem))]
partial struct DebugDrawingSystem : ISystem
{
    private Pathfinder pathfinder;
    private GridNode pathStart;
    private GridNode pathEnd;
    private NativeList<GridNode> path;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CaveGridSystem.Singleton>();
        pathStart = 7;
        pathEnd = 7;
        path = new NativeList<GridNode>(32, Allocator.Persistent);
    }

    public void OnDestroy(ref SystemState state)
    {
        pathfinder.Dispose();
        path.Dispose();
    }

    public void OnUpdate(ref SystemState state)
    {
        var caveGrid = SystemAPI.GetSingletonRW<CaveGridSystem.Singleton>().ValueRW.CaveGrid.AsArray();
        var camera = Camera.main;
        if (camera == null) return;

        if (!pathfinder.IsCreated)
            pathfinder = new Pathfinder(caveGrid.Length, Allocator.Persistent);

        // draw on the cave grid
        if (Input.GetKey(KeyCode.Mouse0) || Input.GetKey(KeyCode.Mouse1))
        {
            var mousePos = camera.ScreenToWorldPoint(Input.mousePosition);
            var mousePosInt = (int2) (math.round(((float3)mousePos).xy+new float2(0.5f, -0.5f)));
            var i = mousePosInt.x + -mousePosInt.y * CaveGridSystem.Singleton.CaveGridWidth;
            if (Input.GetKey(KeyCode.LeftShift))
                caveGrid[i] = Input.GetKey(KeyCode.Mouse0) ? CaveMaterialType.Water : CaveMaterialType.Ore;
            else
                caveGrid[i] = Input.GetKey(KeyCode.Mouse0) ? CaveMaterialType.Air : CaveMaterialType.Rock;

            if (Input.GetKey(KeyCode.Mouse0) && !Input.GetKey(KeyCode.LeftShift))
            {
                pathEnd = i;
                bool foundPath = Pathfinder.FindShortestPath(ref pathfinder, caveGrid, pathStart, pathEnd, ref path);
            }
        }
        
        // camera y up/down from scroll
        if (Input.mouseScrollDelta != Vector2.zero) 
            camera.transform.position += new Vector3(0, Input.mouseScrollDelta.y, 0);
        
        // draw path, if it exists
        for(int i=1; i<path.Length; ++i)
        {
            var x0 = path[i-1] % CaveGridSystem.Singleton.CaveGridWidth;
            var y0 = path[i-1] / CaveGridSystem.Singleton.CaveGridWidth;
            var x1 = path[i] % CaveGridSystem.Singleton.CaveGridWidth;
            var y1 = path[i] / CaveGridSystem.Singleton.CaveGridWidth;
            Debug.DrawLine(
                new float3(x0-0.5f, -y0+0.5f, 0),
                new float3(x1-0.5f, -y1+0.5f, 0),
                Color.green);
        }
        
    }
}

public enum CaveMaterialType
{
    Rock,
    Air,
    Ore,
    Water,
}


[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
public partial struct CaveGridSystem : ISystem, ISystemStartStop
{
    public struct Singleton : IComponentData
    {
        public NativeList<CaveMaterialType> CaveGrid;
        public const int CaveGridWidth = 20;
        public int GetCaveGridHeight() => CaveGrid.Length / CaveGridWidth;
    }

    // Generate the cave grid
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var caveGrid = new NativeList<CaveMaterialType>(Singleton.CaveGridWidth * 128, Allocator.Persistent);
        state.EntityManager.AddComponentData(state.SystemHandle, new Singleton { CaveGrid = caveGrid, });
        caveGrid.Length = caveGrid.Capacity;
        for (var i = 0; i < caveGrid.Length; i++)
        {
            var x = i % Singleton.CaveGridWidth;
            var y = i / Singleton.CaveGridWidth;
            
            // generate cave grid
            var water = math.select(0, (int)CaveMaterialType.Water, noise.cnoise(new float2(x, -y)*0.1f)>0.4f);     // water
            var ore = math.select(0, (int)CaveMaterialType.Ore, noise.cnoise(new float3(x, -y, 0.3f)*0.1f)>0.2f); // ore
            var air = math.select(0, (int)CaveMaterialType.Air, noise.cnoise(new float3(x, -y, 0.6f)*0.1f)>0.3f); // air
            air = math.max(air, y is 0 or 1 && x is 6 or 7 ? (int)CaveMaterialType.Air : (int)CaveMaterialType.Rock);       // cave entrance
            caveGrid[i] = (CaveMaterialType) math.max(math.max(water, ore), air);

#if DEBUG_DRAW_CAVE_GRID
            // Debug draw the cave grid
            Debug.DrawLine(
                new float3(x, -y, 0), 
                new float3(x, -y, 0) + math.forward(), 
                Color.HSVToRGB((float)caveGrid[i]/4f,1,1), 
                100f);
#endif
        }
        
        state.RequireForUpdate<MarchSquareData>();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        SystemAPI.GetComponent<Singleton>(state.SystemHandle).CaveGrid.Dispose();
    }

    // Spawn the tiles
    [BurstCompile]
    public void OnStartRunning(ref SystemState state)
    {
        var spriteTargetPrefab = SystemAPI.GetSingleton<MarchSquareData>().spriteTargetPrefab;
        var caveGridHeight = SystemAPI.GetComponent<Singleton>(state.SystemHandle).GetCaveGridHeight();
        
        for (var y = 0; y > -caveGridHeight + 1; y--)
        {
            for (var x = 0; x < Singleton.CaveGridWidth - 1; x++)
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
        var caveGrid = SystemAPI.GetComponent<Singleton>(state.SystemHandle).CaveGrid.AsArray();
        
        // update outline tile
        foreach (var (lt, offsetXYScaleZwRef) in SystemAPI.Query<LocalToWorld, RefRW<MaterialOverrideOffsetXYScaleZW>>().WithAll<MarchingSquareTileCarverTag>())
        {
            var corners = GetCornerValues((int2)lt.Position.xy, caveGrid);

            // build a 4-bit value from the 4 corners
            corners = 1 - (int4) math.saturate(corners);
            var valCombined = corners.x | (corners.y << 1) | (corners.z << 2) | (corners.w << 3);
            offsetXYScaleZwRef.ValueRW.Value.xy = marchSquareSets[0].GetOffset(valCombined);
        }
        
        // update mat A tile (the one with the highest corner)
        foreach (var (lt, offsetXYScaleZwRef, cornerStrengthRef) in SystemAPI.Query<LocalToWorld, RefRW<MaterialOverrideOffsetXYScaleZW>, RefRW<MaterialOverrideCornerStrength>>().WithAll<MarchingSquareTileMatATag>())
        {
            var corners = GetCornerValues((int2)lt.Position.xy, caveGrid);
            var highestCorners = SortTheFourNumbers(corners);
            cornerStrengthRef.ValueRW.Value = (float4) (corners == highestCorners.w);
            
            // build a 4-bit value from the 4 corners
            corners = 1 - (int4) math.saturate(corners);
            var valCombined = corners.x | (corners.y << 1) | (corners.z << 2) | (corners.w << 3);
            offsetXYScaleZwRef.ValueRW.Value.xy = marchSquareSets[math.clamp(highestCorners.w-1, 0, marchSquareSets.Length-1)].GetOffset(valCombined);
        }
        
        // update mat B tile (same as mat A, but with the second highest corner)
        foreach (var (lt, offsetXYScaleZwRef, cornerStrengthRef) in SystemAPI.Query<LocalToWorld, RefRW<MaterialOverrideOffsetXYScaleZW>, RefRW<MaterialOverrideCornerStrength>>().WithAll<MarchingSquareTileMatBTag>())
        {
            var corners = GetCornerValues((int2)lt.Position.xy, caveGrid);
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
            if (validSecond == 0)
                cornerStrengthRef.ValueRW.Value = 0;
            else
                cornerStrengthRef.ValueRW.Value = (float4) (corners == validSecond);
            
            // build a 4-bit value from the 4 corners
            corners = 1 - (int4) math.saturate(corners);
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
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int4 GetCornerValues(int2 coord, NativeArray<CaveMaterialType> caveGrid)
    {
        // assert that y is negative or ground level. As we're going from top to bottom, y should be negative
        if (coord.y > 0) throw new Exception("y must be negative");

        // get the 4 corners
        var i = coord.x + -coord.y * Singleton.CaveGridWidth;
        return new int4(
            (int)caveGrid[i + Singleton.CaveGridWidth], // Bottom Left
            (int)caveGrid[i + Singleton.CaveGridWidth + 1], // Bottom Right
            (int)caveGrid[i + 1], // Top Right
            (int)caveGrid[i] // Top Left
        );
    }

    public void OnStopRunning(ref SystemState state) {}
}
