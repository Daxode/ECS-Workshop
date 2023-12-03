// #define DEBUG_DRAW_CAVE_GRID
using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.EventSystems;
using static CaveGridSystem.Singleton;

class MarchingSquareAuthor : MonoBehaviour
{
    public MarchSquareSetSprites[] sets;
    public MarchingSquareTile spriteTargetPrefab;
    public AnimatedSpriteAuthor gridLockPrefab;
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
    public Entity gridLockPrefab;
}

class MarchingSquareBaker : Baker<MarchingSquareAuthor>
{
    public override void Bake(MarchingSquareAuthor authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new MarchSquareData
        {
            spriteTargetPrefab = GetEntity(authoring.spriteTargetPrefab, TransformUsageFlags.Renderable),
            gridLockPrefab = GetEntity(authoring.gridLockPrefab, TransformUsageFlags.Renderable)
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

struct Pathfinder : IDisposable
{
    enum NodeState
    {
        Unvisited = 0,
        Seen = 1,
        Visited = 2,
    }
    private NativeArray<NodeState> nodeStates;
    private NativeArray<int> pathCosts; // The total cost of the shortest path to each node from the start that we've found so far. undefined for unvisited nodes
    private NativeArray<int> pathNodeCounts; // The number of nodes in the path from start to each node (including start and this node). undefined for unvisited nodes
    private NativeArray<int> prevNodes; // Index of the previous node in the shortest path from the start to each node. undefined for unvisited nodes
    private NativeList<int> candidateNodes;
    private const int MOVE_COST_LEFTRIGHT = 2;
    private const int MOVE_COST_UP = 3;
    private const int MOVE_COST_DOWN = 1;

    public Pathfinder(int nodeCount, Allocator allocator)
    {
        nodeStates = new NativeArray<NodeState>(nodeCount, allocator, NativeArrayOptions.ClearMemory);
        pathCosts = new NativeArray<int>(nodeCount, allocator, NativeArrayOptions.UninitializedMemory);
        pathNodeCounts = new NativeArray<int>(nodeCount, allocator, NativeArrayOptions.UninitializedMemory);
        prevNodes = new NativeArray<int>(nodeCount, allocator, NativeArrayOptions.UninitializedMemory);
        candidateNodes = new NativeList<int>(nodeCount, allocator);
    }

    public bool IsCreated => nodeStates.IsCreated;
    
    public void Reset()
    {
        for (int i = 0; i < nodeStates.Length; ++i)
            nodeStates[i] = NodeState.Unvisited;
        candidateNodes.Clear();
    }

    public void Dispose()
    {
        if (!IsCreated)
            return;
        nodeStates.Dispose();
        pathCosts.Dispose();
        pathNodeCounts.Dispose();
        prevNodes.Dispose();
        candidateNodes.Dispose();
    }

    void ProcessNeighbor(int currentNode, int neighbor, int newCost)
    {
        if (nodeStates[neighbor] == NodeState.Unvisited)
        {
            nodeStates[neighbor] = NodeState.Seen;
            pathCosts[neighbor] = newCost;
            pathNodeCounts[neighbor] = pathNodeCounts[currentNode] + 1;
            prevNodes[neighbor] = currentNode;
            candidateNodes.AddNoResize(neighbor);
        }
        else if (nodeStates[neighbor] == NodeState.Seen && newCost < pathCosts[neighbor])
        {
            pathCosts[neighbor] = newCost;
            pathNodeCounts[neighbor] = pathNodeCounts[currentNode] + 1;
            prevNodes[neighbor] = currentNode;
            // TODO: reorder in candidates queue
        }
    }

    /// <summary>
    /// Finds the shortest path between two points on the grid.
    /// </summary>
    /// <param name="caveGrid">The current grid state.</param>
    /// <param name="start">index of the starting point on the grid.</param>
    /// <param name="end">index of the destination on the grid.</param>
    /// <param name="outPath">Contains the sequence of nodes to visit to reach <paramref name="end"/> from
    /// <paramref name="start"/>, including these two nodes themselves. If no path is found, this list will be
    /// empty.</param>
    /// <returns>True if a path from start to end was found, or false if end is not reachable from start.</returns>
    public bool FindShortestPath(in NativeArray<CaveMaterialType> caveGrid, int start, int end, ref NativeList<int> outPath)
    {
        outPath.Clear();
        
        // Handle the degenerate cases:
        if (caveGrid[start] != CaveMaterialType.Air ||
            caveGrid[end] != CaveMaterialType.Air)
        {
            return false;
        }
        if (start == end)
        {
            outPath.Add(start);
            return true;
        }

        Reset();
        pathCosts[start] = 0;
        pathNodeCounts[start] = 1;
        prevNodes[start] = -1;
        candidateNodes.AddNoResize(start);
        while (!candidateNodes.IsEmpty)
        {
            // Find the candidate node with the shortest overall path length.
            int nextCandidateIndex = 0;
            int nextCandidatePathCost = pathCosts[candidateNodes[nextCandidateIndex]];
            for (int i = 0; i < candidateNodes.Length; ++i)
            {
                if (pathCosts[candidateNodes[i]] < nextCandidatePathCost)
                {
                    nextCandidatePathCost = pathCosts[candidateNodes[i]];
                    nextCandidateIndex = i;
                }
            }
            // As soon as the end node *could* be the next candidate, the search is complete. We don't
            // actually need to wait until we visit that node.
            if (Hint.Unlikely(nodeStates[end] == NodeState.Seen && pathCosts[end] == nextCandidatePathCost))
            {
                outPath.ResizeUninitialized(pathNodeCounts[end]);
                int n = end;
                for (int i = outPath.Length - 1; i >= 0; --i)
                {
                    outPath[i] = n;
                    n = prevNodes[n];
                }
                return true;
            }
            int currentNode = candidateNodes[nextCandidateIndex];
            candidateNodes.RemoveAtSwapBack(nextCandidateIndex);
            // The current node's path length & previous node are now correct.
            nodeStates[currentNode] = NodeState.Visited;
            // Check any accessible neighbors.
            // If this is the first time we've seen them, add them to the candidates list.
            // If the path to the neighbor through the current node is shorter than what we've seen so far,
            // update the neighbor's path length & previous node.
            int currentLength = pathCosts[currentNode];
            var x = currentNode % CaveGridSystem.Singleton.CaveGridWidth;
            var y = currentNode / CaveGridSystem.Singleton.CaveGridWidth;
            var neighborL = currentNode - 1;
            var neighborR = currentNode + 1;
            var neighborU = currentNode - CaveGridSystem.Singleton.CaveGridWidth;
            var neighborD = currentNode + CaveGridSystem.Singleton.CaveGridWidth;
            if (x > 0 && caveGrid[neighborL] == CaveMaterialType.Air)
                ProcessNeighbor(currentNode, neighborL, currentLength + MOVE_COST_LEFTRIGHT);
            if (x < CaveGridSystem.Singleton.CaveGridWidth-1 && caveGrid[neighborR] == CaveMaterialType.Air)
                ProcessNeighbor(currentNode, neighborR, currentLength + MOVE_COST_LEFTRIGHT);
            if (y > 0 && caveGrid[neighborU] == CaveMaterialType.Air)
                ProcessNeighbor(currentNode, neighborU, currentLength + MOVE_COST_UP);
            if (y < CaveGridSystem.Singleton.CaveGridWidth-1 && caveGrid[neighborD] == CaveMaterialType.Air)
                ProcessNeighbor(currentNode, neighborD, currentLength + MOVE_COST_DOWN);
        }
        // If the candidates array empties without visiting the end node, it means the end node isn't reachable
        // from the start node.
        return false;
    }
}

[UpdateBefore(typeof(CaveGridSystem))]
partial struct DebugDrawingSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CursorSelection>();
        state.RequireForUpdate<CaveGridSystem.Singleton>();
        state.RequireForUpdate<ConstructionSiteElement>();
        state.RequireForUpdate<MarchSquareData>();
    }
    
    public void OnUpdate(ref SystemState state)
    {
        var camera = Camera.main;
        if (camera == null) return;
        var caveSystem = SystemAPI.GetSingletonRW<CaveGridSystem.Singleton>().ValueRW;
        var caveGrid = caveSystem.CaveGrid.AsArray();
        var caveTiles = caveSystem.CaveTiles.AsArray();

        var constructionSites = SystemAPI.GetSingletonBuffer<ConstructionSiteElement>();

        ref var cursorToDraw = ref SystemAPI.GetSingletonRW<CursorSelection>().ValueRW.cursorToDraw;

        if (Input.GetKeyDown(KeyCode.Mouse1) && cursorToDraw.IsOutline())
            cursorToDraw.SetDefault();
        
        if (Input.GetKeyDown(KeyCode.Mouse0) && cursorToDraw.IsOutline())
        {
            // Get the tile index
            float3 mousePos = camera.ScreenToWorldPoint(Input.mousePosition);
            var tileIndex = WorldPosToTileIndex(mousePos.xy);

            if (tileIndex >= 0 && tileIndex < caveTiles.Length && caveTiles[tileIndex] == Entity.Null) {
                // check if valid
                var corners = caveGrid.GetCornerValues((int2)(math.round(mousePos.xy)));
                corners = 1 - (int4)math.saturate(corners);
                var marchSetIndex = corners.x | (corners.y << 1) | (corners.z << 2) | (corners.w << 3);
                if ((marchSetIndex == 3 && cursorToDraw is CursorSelection.CursorToDraw.StockpileOutline or CursorSelection.CursorToDraw.WorkshopOutline) 
                    || marchSetIndex == 0 && cursorToDraw is CursorSelection.CursorToDraw.LadderOutline)
                {
                    var constructionIndex = (cursorToDraw - CursorSelection.CursorToDraw.LadderOutline);
                    var constructionSiteEntity = state.EntityManager.Instantiate(constructionSites[constructionIndex].constructionSite);
                    SystemAPI.SetComponent(constructionSiteEntity, LocalTransform.FromPosition(new float3(math.round(mousePos.xy), -2)));
                    caveTiles[tileIndex] = constructionSiteEntity;

                    // deselect the cursor
                    cursorToDraw.SetDefault();
                }
            }
        }
        
        // Queue the grid rock to be destroyed
        if (Input.GetKeyDown(KeyCode.Mouse0) && cursorToDraw.IsDestroy())
        {
            // Get the grid index
            float3 mousePos = camera.ScreenToWorldPoint(Input.mousePosition);
            var (snappedPos, snappedToTile) = SnapToTileOrGrid(mousePos.xy);

            if (snappedToTile)
            {
                var tileIndex = WorldPosToTileIndex(snappedPos);
                if (tileIndex >= 0 && tileIndex < caveTiles.Length && caveTiles[tileIndex] != Entity.Null) {
                    if (SystemAPI.HasComponent<ConstructionSite>(caveTiles[tileIndex]))
                    {
                        state.EntityManager.DestroyEntity(caveTiles[tileIndex]);
                        caveTiles[tileIndex] = Entity.Null;
                    }
                }
            }
            else
            {
                var gridIndex = WorldPosToGridIndex(snappedPos);
                if (gridIndex >= 0 && gridIndex < caveGrid.Length && caveGrid[gridIndex] != CaveMaterialType.Air) {
                    var gridLockPrefab = SystemAPI.GetSingleton<MarchSquareData>().gridLockPrefab;
                    var gridLockEntity = state.EntityManager.Instantiate(gridLockPrefab);
                    SystemAPI.SetComponent(gridLockEntity, 
                        LocalTransform.FromPosition(new float3(SnapWorldPosToGridPos(mousePos.xy), -2)));
                }
            }

        }
        
        // draw on the cave grid
        if ((Input.GetKey(KeyCode.Mouse0) || Input.GetKey(KeyCode.Mouse1)) && cursorToDraw.IsDrawn())
        {
            float3 mousePos = camera.ScreenToWorldPoint(Input.mousePosition);
            var i = CaveGridSystem.Singleton.WorldPosToGridIndex(mousePos.xy);
            if (i >= 0 && i < caveGrid.Length)
            {
                if (Input.GetKey(KeyCode.LeftShift))
                    caveGrid[i] = Input.GetKey(KeyCode.Mouse0) ? CaveMaterialType.Water : CaveMaterialType.Ore;
                else
                    caveGrid[i] = Input.GetKey(KeyCode.Mouse0) ? CaveMaterialType.Air : CaveMaterialType.Rock;
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

public enum CaveMaterialType : byte
{
    Rock,
    Air,
    Ore,
    Water,
}


[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
[UpdateAfter(typeof(TransformSystemGroup))]
public partial struct CaveGridSystem : ISystem, ISystemStartStop
{
    public struct Singleton : IComponentData
    {
        public NativeList<CaveMaterialType> CaveGrid;
        public const int CaveGridWidth = 20;
        public int GetCaveGridHeight() => CaveGrid.Length / CaveGridWidth;
        
        public NativeList<Entity> CaveTiles;
        public const int CaveTilesWidth = CaveGridWidth-1;
        public int GetCaveTileHeight() => CaveTiles.Length / CaveTilesWidth;
        
        public static int WorldPosToTileIndex(float2 worldPos)
        {
            var tilePos = (int2)math.round(worldPos);
            return tilePos.x + -tilePos.y * CaveTilesWidth;
        }
    
        public static int2 WorldPosToGridPos(float2 worldPos) => 
            (int2) math.round(worldPos+new float2(0.5f, -0.5f));
        public static float2 SnapWorldPosToGridPos(float2 worldPos) => 
            WorldPosToGridPos(worldPos) - new float2(0.5f, -0.5f);

        public static int WorldPosToGridIndex(float2 worldPos)
        {
            var gridPos = WorldPosToGridPos(worldPos);
            return gridPos.x + -gridPos.y * CaveGridWidth;
        }
        
        public static (float2 pos, bool snappedToTile) SnapToTileOrGrid(float2 worldPos)
        {
            var gridPos = SnapWorldPosToGridPos(worldPos);
            var tilePos = math.round(worldPos);
            var snappedToTile = math.distancesq(worldPos, tilePos) < math.distancesq(worldPos, gridPos);
            return (math.select(gridPos, tilePos, snappedToTile), snappedToTile);
        }
    }

    // Generate the cave grid
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var caveGrid = new NativeList<CaveMaterialType>(CaveGridWidth * 128, Allocator.Persistent);
        var caveTiles = new NativeList<Entity>(CaveTilesWidth * 127, Allocator.Persistent);
        state.EntityManager.AddComponentData(state.SystemHandle, new Singleton { CaveGrid = caveGrid, CaveTiles = caveTiles});
        caveGrid.Length = caveGrid.Capacity;
        caveTiles.Resize(caveTiles.Capacity, NativeArrayOptions.ClearMemory);
        for (var i = 0; i < caveGrid.Length; i++)
        {
            var x = i % CaveGridWidth;
            var y = i / CaveGridWidth;
            
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
            for (var x = 0; x < CaveGridWidth - 1; x++)
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
            var corners = caveGrid.GetCornerValues((int2)lt.Position.xy);

            // build a 4-bit value from the 4 corners
            corners = 1 - (int4) math.saturate(corners);
            var valCombined = corners.x | (corners.y << 1) | (corners.z << 2) | (corners.w << 3);
            offsetXYScaleZwRef.ValueRW.Value.xy = marchSquareSets[0].GetOffset(valCombined);
        }
        
        // update mat A tile (the one with the highest corner)
        foreach (var (lt, offsetXYScaleZwRef, cornerStrengthRef) in SystemAPI.Query<LocalToWorld, RefRW<MaterialOverrideOffsetXYScaleZW>, RefRW<MaterialOverrideCornerStrength>>().WithAll<MarchingSquareTileMatATag>())
        {
            var corners = caveGrid.GetCornerValues((int2)lt.Position.xy);
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
            var corners = caveGrid.GetCornerValues((int2)lt.Position.xy);
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

    public void OnStopRunning(ref SystemState state) {}
}

static class CaveSystemExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int4 GetCornerValues(this NativeArray<CaveMaterialType> caveGrid, int2 coord)
    {
        // assert that y is negative or ground level. As we're going from top to bottom, y should be negative
        if (coord.y > 0) throw new Exception("y must be negative");

        // get the 4 corners
        var i = coord.x + -coord.y * CaveGridWidth;
        return new int4(
            (int)caveGrid[i + CaveGridWidth], // Bottom Left
            (int)caveGrid[i + CaveGridWidth + 1], // Bottom Right
            (int)caveGrid[i + 1], // Top Right
            (int)caveGrid[i] // Top Left
        );
    }
}
