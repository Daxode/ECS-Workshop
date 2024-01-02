using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

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

/// <summary>
/// 
/// </summary>
public struct PathGoal : IComponentData, IEnableableComponent
{
    public GridNodeIndex nodeIndex;
}

public struct PathMoveState : IComponentData, IEnableableComponent
{
    public GridNodeIndex From, To;
    public float T; // 0..1
}

/// <summary>
/// The nodes comprising a path to the target node stored in the <see cref="PathGoal"/> component. 
/// </summary>
[InternalBufferCapacity(0)]
public struct PathNode : IBufferElementData, IEnableableComponent
{
    public GridNodeIndex nodeIndex;
    public static implicit operator PathNode(GridNodeIndex n) => new PathNode { nodeIndex = n };
    public static implicit operator GridNodeIndex(PathNode n) => n.nodeIndex;
}

[BurstCompile]
public struct Pathfinder : IDisposable
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
    private NativeArray<GridNodeIndex> prevNodes; // Index of the previous node in the shortest path from the start to each node. undefined for unvisited nodes
    private NativeList<GridNodeIndex> candidateNodes;
    private const int MOVE_COST_LEFTRIGHT = 2;
    private const int MOVE_COST_UP = 3;
    private const int MOVE_COST_DOWN = 1;

    public Pathfinder(int nodeCount, Allocator allocator)
    {
        nodeStates = new NativeArray<NodeState>(nodeCount, allocator, NativeArrayOptions.ClearMemory);
        pathCosts = new NativeArray<int>(nodeCount, allocator, NativeArrayOptions.UninitializedMemory);
        pathNodeCounts = new NativeArray<int>(nodeCount, allocator, NativeArrayOptions.UninitializedMemory);
        prevNodes = new NativeArray<GridNodeIndex>(nodeCount, allocator, NativeArrayOptions.UninitializedMemory);
        candidateNodes = new NativeList<GridNodeIndex>(nodeCount, allocator);
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

    void ProcessNeighbor(GridNodeIndex currentNodeIndex, GridNodeIndex neighbor, int newCost)
    {
        if (nodeStates[neighbor] == NodeState.Unvisited)
        {
            nodeStates[neighbor] = NodeState.Seen;
            pathCosts[neighbor] = newCost;
            pathNodeCounts[neighbor] = pathNodeCounts[currentNodeIndex] + 1;
            prevNodes[neighbor] = currentNodeIndex;
            candidateNodes.AddNoResize(neighbor);
        }
        else if (nodeStates[neighbor] == NodeState.Seen && newCost < pathCosts[neighbor])
        {
            pathCosts[neighbor] = newCost;
            pathNodeCounts[neighbor] = pathNodeCounts[currentNodeIndex] + 1;
            prevNodes[neighbor] = currentNodeIndex;
            // TODO: reorder in candidates queue
        }
    }

    /// <summary>
    /// Finds the shortest path between two points on the grid.
    /// </summary>
    /// <param name="pathfinder">The pathfinder object to use for this search. Must not be in use by any other thread.</param>
    /// <param name="caveGrid">The current grid state.</param>
    /// <param name="start">index of the starting point on the grid.</param>
    /// <param name="end">index of the destination on the grid.</param>
    /// <param name="outPath">
    /// Contains the sequence of nodes to visit to reach <paramref name="end"/> from <paramref name="start"/>.
    /// The sequence will be in reverse order, as this simplifies both the code to generate the list and the code
    /// to consume it.
    /// If no path is found (or if the start and end nodes are equal), this list will be empty. Otherwise, the first
    /// element will always be <paramref name="end"/>. The <paramref name="start"/> node will not be included in the
    /// list.</param>
    [BurstCompile]
    public static void FindShortestPath(ref Pathfinder pathfinder,in NativeArray<NavigationSystem.NodeType> caveGrid, GridNodeIndex start, GridNodeIndex end,
        ref DynamicBuffer<PathNode> outPath)
    {
        pathfinder.FindShortestPath(caveGrid, start, end, ref outPath);
    }

    void FindShortestPath(in NativeArray<NavigationSystem.NodeType> caveGrid, GridNodeIndex start, GridNodeIndex end, ref DynamicBuffer<PathNode> outPath)
    {
        outPath.Clear();
        
        // Handle the degenerate cases:
        if (caveGrid[start] != NavigationSystem.NodeType.Ground ||
            caveGrid[end] != NavigationSystem.NodeType.Ground)
        {
            return; // no path from start to end
        }
        if (start == end)
        {
            return; // start and end are identical
        }

        Reset();
        pathCosts[start] = 0;
        pathNodeCounts[start] = 0;
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
                outPath.Capacity = pathNodeCounts[end];
                GridNodeIndex n = end;
                do
                {
                    if (caveGrid[n].IsGround()) 
                        outPath.Add(n);
                    n = prevNodes[n];
                } while (n != start);
                return;
            }
            GridNodeIndex currentNodeIndex = candidateNodes[nextCandidateIndex];
            candidateNodes.RemoveAtSwapBack(nextCandidateIndex);
            // The current node's path length & previous node are now correct.
            nodeStates[currentNodeIndex] = NodeState.Visited;
            // Check any accessible neighbors.
            // If this is the first time we've seen them, add them to the candidates list.
            // If the path to the neighbor through the current node is shorter than what we've seen so far,
            // update the neighbor's path length & previous node.
            int currentLength = pathCosts[currentNodeIndex];
            var x = currentNodeIndex % NavigationSystem.navWidth;
            var y = currentNodeIndex / NavigationSystem.navWidth;
            GridNodeIndex neighborL = currentNodeIndex - 1;
            GridNodeIndex neighborR = currentNodeIndex + 1;
            GridNodeIndex neighborU = currentNodeIndex - NavigationSystem.navWidth;
            GridNodeIndex neighborD = currentNodeIndex + NavigationSystem.navWidth;

            // left
            if (x > 0 && !caveGrid[neighborL].IsObstructed())
                ProcessNeighbor(currentNodeIndex, neighborL, currentLength + MOVE_COST_LEFTRIGHT);
            // right
            if (x < NavigationSystem.navWidth - 1 && !caveGrid[neighborR].IsObstructed())
                ProcessNeighbor(currentNodeIndex, neighborR, currentLength + MOVE_COST_LEFTRIGHT);
            // up
            if (caveGrid[currentNodeIndex] is not NavigationSystem.NodeType.JumpDown)
                if (y > 0 && !caveGrid[neighborU].IsObstructed()) 
                    ProcessNeighbor(currentNodeIndex, neighborU, currentLength + MOVE_COST_UP);
                
            var jumpModeIsLeft = caveGrid[currentNodeIndex] is NavigationSystem.NodeType.GroundLedgeL;
            if (jumpModeIsLeft || caveGrid[currentNodeIndex] is NavigationSystem.NodeType.GroundLedgeR)
            {
                // loop down until hit mid landing
                var jumpTo = neighborD;
                while (caveGrid[jumpTo] is not NavigationSystem.NodeType.GroundLandingMid) 
                    jumpTo += NavigationSystem.navWidth;
                if (jumpModeIsLeft)
                    jumpTo -= 1;
                else
                    jumpTo += 1;
                ProcessNeighbor(currentNodeIndex, jumpTo, currentLength + MOVE_COST_DOWN);
            }
            else 
            {
                // down
                if (y < NavigationSystem.navWidth - 1 && !caveGrid[neighborD].IsObstructed())
                    ProcessNeighbor(currentNodeIndex, neighborD, currentLength + MOVE_COST_DOWN);
            }
        }
        // If the candidates array empties without visiting the end node, it means the end node isn't reachable
        // from the start node.
        return;
    }
}


static class NavigationExtensions
{
    public struct NavigationIndexes
    {
        public int gridTL, gridTC, gridTR;
        public int gridCL, gridCC, gridCR;
        public int gridBL, gridBC, gridBR;
        
        public static NavigationIndexes FromTilePos(int x, int y)
        {
            var gridIndex = (x * 2) + (-y * 2 * NavigationSystem.navWidth);
            return new NavigationIndexes
            {
                gridTL = gridIndex,
                gridTC = gridIndex + 1,
                gridTR = gridIndex + 2,
                gridCL = gridIndex + NavigationSystem.navWidth,
                gridCC = gridIndex + NavigationSystem.navWidth + 1,
                gridCR = gridIndex + NavigationSystem.navWidth + 2,
                gridBL = gridIndex + NavigationSystem.navWidth * 2,
                gridBC = gridIndex + NavigationSystem.navWidth * 2 + 1,
                gridBR = gridIndex + NavigationSystem.navWidth * 2 + 2
            };
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetNavigation(this NativeList<NavigationSystem.NodeType> navGrid, ref NavigationIndexes indexes,
        NavigationSystem.NodeType tl, NavigationSystem.NodeType tc, NavigationSystem.NodeType tr, 
        NavigationSystem.NodeType cl, NavigationSystem.NodeType cc, NavigationSystem.NodeType cr, 
        NavigationSystem.NodeType bl, NavigationSystem.NodeType bc, NavigationSystem.NodeType br)
    {
        navGrid[indexes.gridTL] = tl;
        navGrid[indexes.gridTC] = tc;
        navGrid[indexes.gridTR] = tr;
        navGrid[indexes.gridCL] = cl;
        navGrid[indexes.gridCC] = cc;
        navGrid[indexes.gridCR] = cr;
        navGrid[indexes.gridBL] = bl;
        navGrid[indexes.gridBC] = bc;
        navGrid[indexes.gridBR] = br;
    }
}

static class NodeTypeExtensions
{
    public static bool IsGround(this NavigationSystem.NodeType nodeType) => nodeType
        is NavigationSystem.NodeType.Ground
        or NavigationSystem.NodeType.GroundLandingR
        or NavigationSystem.NodeType.GroundLandingL
        or NavigationSystem.NodeType.GroundLandingLR
        or NavigationSystem.NodeType.GroundLandingMid
        or NavigationSystem.NodeType.GroundLedgeR
        or NavigationSystem.NodeType.GroundLedgeL;

    // is not navigatable
    public static bool IsObstructed(this NavigationSystem.NodeType nodeType) => nodeType 
        is NavigationSystem.NodeType.Air 
        or NavigationSystem.NodeType.Obstructed;
}

public partial struct NavigationSystem : ISystem, ISystemStartStop
{
    NativeList<NodeType> m_NavigationGrid;

    public enum NodeType : ushort
    {
        Air,
        Obstructed,
        Ground,
        GroundLedgeL,
        GroundLedgeR,
        GroundLandingL,
        GroundLandingR,
        GroundLandingLR,
        GroundLandingMid,
        JumpDown,
        JumpUpDown,
    }

    public const int navWidth = (CaveGridSystem.Singleton.CaveGridWidth * 2) - 1;
    Pathfinder m_Pathfinder;
    
    public void OnCreate(ref SystemState state)
    {
        m_NavigationGrid = new NativeList<NodeType>(navWidth*(128*2-1), Allocator.Persistent);
        m_NavigationGrid.Resize(navWidth*(128*2-1), NativeArrayOptions.ClearMemory);
        m_Pathfinder = new Pathfinder(m_NavigationGrid.Length, Allocator.Persistent);
        
        state.RequireForUpdate<MarchSquareData>();
        state.RequireForUpdate<CaveGridSystem.Singleton>();
    }

    public void OnDestroy(ref SystemState state)
    {
        m_Pathfinder.Dispose();
    }

    
    public void OnStartRunning(ref SystemState state)
    {
        var gridLockPrefab = SystemAPI.GetSingleton<MarchSquareData>().gridLockPrefab;
        var gridHeight = m_NavigationGrid.Length / navWidth;
        for (var y = 0; y > -gridHeight; y--)
        {
            for (var x = 0; x < navWidth; x++)
            {
                // spawn a sprite with the correct offset
                var spriteTarget = state.EntityManager.Instantiate(gridLockPrefab);
                var pos = new float3(new float2(x - 1, y + 1) * 0.5f, -5);
                SystemAPI.SetComponent(spriteTarget, LocalTransform.FromPosition(pos));
                SystemAPI.SetComponent(spriteTarget, new LocalToWorld{Value = float4x4.Translate(pos)});
                state.EntityManager.AddComponentData(spriteTarget, new MaterialOverrideCornerStrength
                {
                    Value = (Vector4)Color.magenta
                });
                state.EntityManager.AddComponentData(spriteTarget, new NavigationGridLockTag());
            }
        }
    }
    
    struct NavigationGridLockTag : IComponentData {}
    
    bool showDebugGrid;

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (Input.GetKeyDown(KeyCode.G))
            showDebugGrid = !showDebugGrid;
        
        var caveSingleton = SystemAPI.GetSingleton<CaveGridSystem.Singleton>();
        var caveTileHeight = caveSingleton.GetCaveTileHeight();
        var corners = caveSingleton.CaveGrid.AsArray();
        var tiles = caveSingleton.CaveTiles.AsArray();
        for (var tileY = 0; tileY > -caveTileHeight; tileY--)
        {
            for (var tileX = 0; tileX < CaveGridSystem.Singleton.CaveTilesWidth; tileX++)
            {
                var cornerIndex = tileX + (-tileY * CaveGridSystem.Singleton.CaveGridWidth);
                var tl = corners[cornerIndex] == CaveMaterialType.Air ? 0 : 1;
                var tr = corners[cornerIndex + 1] == CaveMaterialType.Air ? 0 : 1;
                var bl = corners[cornerIndex + CaveGridSystem.Singleton.CaveGridWidth] == CaveMaterialType.Air ? 0 : 1;
                var br = corners[cornerIndex + CaveGridSystem.Singleton.CaveGridWidth + 1] == CaveMaterialType.Air ? 0 : 1;
                var marchSquareIndex = bl | (br << 1) | (tr << 2) | (tl << 3);
                
                var navIndexes = NavigationExtensions.NavigationIndexes.FromTilePos(tileX, tileY);
                // Switch on the 4 corners of the square to select where ground is
                switch (marchSquareIndex)
                {
                    case 0: // 0000
                        m_NavigationGrid.SetNavigation(ref navIndexes,
                                NodeType.Air, NodeType.Air, NodeType.Air,
                                NodeType.Air, NodeType.Air, NodeType.Air,
                                NodeType.Air, NodeType.Air, NodeType.Air);
                        break;
                    case 1: // 0001
                        m_NavigationGrid.SetNavigation(ref navIndexes,
                            NodeType.Air, NodeType.Air, NodeType.Air,
                            NodeType.Ground, NodeType.GroundLedgeR, NodeType.Air,
                            NodeType.Obstructed, NodeType.Air, NodeType.Air);
                        break;
                    case 2: // 0010
                        m_NavigationGrid.SetNavigation(ref navIndexes,
                            NodeType.Air, NodeType.Air, NodeType.Air,
                            NodeType.Air, NodeType.GroundLedgeL, NodeType.Ground,
                            NodeType.Air, NodeType.Air, NodeType.Obstructed);
                        break;
                    case 3: // 0011
                        m_NavigationGrid.SetNavigation(ref navIndexes,
                            NodeType.Air, NodeType.Air, NodeType.Air,
                            NodeType.Ground, NodeType.Ground, NodeType.Ground,
                            NodeType.Obstructed, NodeType.Obstructed, NodeType.Obstructed);
                        break;
                    case 4: // 0100
                        m_NavigationGrid.SetNavigation(ref navIndexes,
                            NodeType.Air, NodeType.Air, NodeType.Obstructed,
                            NodeType.Air, NodeType.Air, NodeType.Air,
                            NodeType.Air, NodeType.Air, NodeType.Air);
                        break;
                    case 5: // 0101
                        m_NavigationGrid.SetNavigation(ref navIndexes,
                            NodeType.Air, NodeType.Air, NodeType.Obstructed,
                            NodeType.Ground, NodeType.Ground, NodeType.Air,
                            NodeType.Obstructed, NodeType.Air, NodeType.Air);
                        break;
                    case 6: // 0110
                        m_NavigationGrid.SetNavigation(ref navIndexes,
                            NodeType.Air, NodeType.Air, NodeType.Obstructed,
                            NodeType.Air, NodeType.Air, NodeType.Obstructed,
                            NodeType.Air, NodeType.Air, NodeType.Obstructed);
                        break;
                    case 7: // 0111
                        m_NavigationGrid.SetNavigation(ref navIndexes,
                            NodeType.Air, NodeType.Air, NodeType.Obstructed,
                            NodeType.Ground, NodeType.Ground, NodeType.Obstructed,
                            NodeType.Obstructed, NodeType.Obstructed, NodeType.Obstructed);
                        break;
                    case 8: // 1000
                        m_NavigationGrid.SetNavigation(ref navIndexes,
                            NodeType.Obstructed, NodeType.Air, NodeType.Air,
                            NodeType.Air, NodeType.Air, NodeType.Air,
                            NodeType.Air, NodeType.Air, NodeType.Air);
                        break;
                    case 9: // 1001
                        m_NavigationGrid.SetNavigation(ref navIndexes,
                            NodeType.Obstructed, NodeType.Air, NodeType.Air,
                            NodeType.Obstructed, NodeType.Air, NodeType.Air,
                            NodeType.Obstructed, NodeType.Air, NodeType.Air);
                        break;
                    case 10: // 1010
                        m_NavigationGrid.SetNavigation(ref navIndexes,
                            NodeType.Obstructed, NodeType.Air, NodeType.Air,
                            NodeType.Air, NodeType.Ground, NodeType.Ground,
                            NodeType.Air, NodeType.Air, NodeType.Obstructed);
                        break;
                    case 11: // 1011
                        m_NavigationGrid.SetNavigation(ref navIndexes,
                            NodeType.Obstructed, NodeType.Air, NodeType.Air,
                            NodeType.Obstructed, NodeType.Ground, NodeType.Ground,
                            NodeType.Obstructed, NodeType.Obstructed, NodeType.Obstructed);
                        break;
                    case 12: // 1100
                        m_NavigationGrid.SetNavigation(ref navIndexes,
                            NodeType.Obstructed, NodeType.Obstructed, NodeType.Obstructed,
                            NodeType.Air, NodeType.Air, NodeType.Air,
                            NodeType.Air, NodeType.Air, NodeType.Air);
                        break;
                    case 13: // 1101
                        m_NavigationGrid.SetNavigation(ref navIndexes,
                            NodeType.Obstructed, NodeType.Obstructed, NodeType.Obstructed,
                            NodeType.Obstructed, NodeType.Air, NodeType.Air,
                            NodeType.Obstructed, NodeType.Air, NodeType.Air);
                        break;
                    case 14: // 1110
                        m_NavigationGrid.SetNavigation(ref navIndexes,
                            NodeType.Obstructed, NodeType.Obstructed, NodeType.Obstructed,
                            NodeType.Air, NodeType.Air, NodeType.Obstructed,
                            NodeType.Air, NodeType.Air, NodeType.Obstructed);
                        break;
                    case 15: // 1111
                        m_NavigationGrid.SetNavigation(ref navIndexes,
                            NodeType.Obstructed, NodeType.Obstructed, NodeType.Obstructed,
                            NodeType.Obstructed, NodeType.Obstructed, NodeType.Obstructed,
                            NodeType.Obstructed, NodeType.Obstructed, NodeType.Obstructed);
                        break;
                }
            }
        }
        
        for (var tileY = 0; tileY > -caveTileHeight; tileY--)
        {
            for (var tileX = 0; tileX < CaveGridSystem.Singleton.CaveTilesWidth; tileX++)
            {
                var navIndexes = NavigationExtensions.NavigationIndexes.FromTilePos(tileX, tileY);
                var tileEntity = tiles[tileX + (-tileY * CaveGridSystem.Singleton.CaveTilesWidth)];
                if (tileEntity != Entity.Null && !SystemAPI.HasComponent<ConstructionSite>(tileEntity))
                    m_NavigationGrid.SetNavigation(ref navIndexes,
                        NodeType.Air, NodeType.JumpDown, NodeType.Air,
                        NodeType.JumpDown, NodeType.Ground, NodeType.JumpDown,
                        NodeType.Air, NodeType.JumpDown, NodeType.Air);

            }
        }

        // Find green, and if blue up and down, set all blues to jump down until first non blue
        for (int navIndex = 0; navIndex < m_NavigationGrid.Length; navIndex++)
        {
            var isLeft = m_NavigationGrid[navIndex] is NodeType.GroundLedgeL or NodeType.GroundLandingL;
            if (isLeft || m_NavigationGrid[navIndex] is NodeType.GroundLedgeR or NodeType.GroundLandingR)
            {
                var neighborU = navIndex - navWidth;
                var current = navIndex;
                var neighborD = navIndex + navWidth;
                if (neighborU < 0 || neighborD >= m_NavigationGrid.Length) continue;

                while (m_NavigationGrid[neighborU] is NodeType.Air or NodeType.JumpDown or NodeType.GroundLedgeL or NodeType.GroundLedgeR or NodeType.GroundLandingL or NodeType.GroundLandingR
                       && m_NavigationGrid[neighborD] is NodeType.Air or NodeType.JumpDown)
                {
                    m_NavigationGrid[neighborD] = NodeType.JumpDown;
                    
                    neighborU = current;
                    current = neighborD;
                    neighborD += navWidth;
                    if (neighborD >= m_NavigationGrid.Length) break;
                }
                // ground and not ledge
                if (m_NavigationGrid[neighborD].IsGround() && m_NavigationGrid[neighborD] is not NodeType.GroundLedgeL and not NodeType.GroundLedgeR)
                    m_NavigationGrid[neighborD] = NodeType.GroundLandingMid;
                if (isLeft)
                {
                    if (m_NavigationGrid[neighborD - 1] is NodeType.Ground or NodeType.GroundLedgeL) 
                        m_NavigationGrid[neighborD - 1] = NodeType.GroundLandingL;
                    if (m_NavigationGrid[neighborD - 1] is NodeType.GroundLedgeR or NodeType.GroundLandingR)
                        m_NavigationGrid[neighborD - 1] = NodeType.GroundLandingLR;
                }
                else
                {
                    if (m_NavigationGrid[neighborD + 1] is NodeType.Ground or NodeType.GroundLedgeR)
                        m_NavigationGrid[neighborD + 1] = NodeType.GroundLandingR;
                    if (m_NavigationGrid[neighborD + 1] is NodeType.GroundLedgeL or NodeType.GroundLandingL)
                        m_NavigationGrid[neighborD + 1] = NodeType.GroundLandingLR;
                }
            }
        }
        
        // Find jump down and set jump up if there is ground above
        for (int navIndex = 0; navIndex < m_NavigationGrid.Length; navIndex++)
        {
            if (m_NavigationGrid[navIndex] == NodeType.JumpDown)
            {
                var neighborU = navIndex - navWidth;
                var neighborD = navIndex + navWidth;
                if (neighborU < 0 || neighborD >= m_NavigationGrid.Length) continue;
                if (m_NavigationGrid[neighborU].IsGround() && m_NavigationGrid[neighborD].IsGround())
                    m_NavigationGrid[navIndex] = NodeType.JumpUpDown;
            }
        }
        
        // change color based on grid
        foreach (var (lt, colorRef) in SystemAPI.Query<LocalToWorld, RefRW<MaterialOverrideCornerStrength>>().WithAll<NavigationGridLockTag>())
        {
            if (!showDebugGrid)
            {
                colorRef.ValueRW.Value = (Vector4)Color.clear;
                continue;
            }
            
            var gridPos = lt.Position.xy * 2 + new float2(1, -1);
            var navigationGridIndex = (int)gridPos.x + (int)-gridPos.y * navWidth;
            if (Input.GetKey(KeyCode.W))
                colorRef.ValueRW.Value = m_NavigationGrid[navigationGridIndex].IsGround()
                    ? (Vector4)Color.green
                    : (Vector4)Color.red;
            else if (Input.GetKey(KeyCode.E))
                colorRef.ValueRW.Value = m_NavigationGrid[navigationGridIndex].IsObstructed()
                    ? (Vector4)Color.red
                    : (Vector4)Color.green;
            else if (Input.GetKey(KeyCode.D))
                colorRef.ValueRW.Value = m_NavigationGrid[navigationGridIndex]
                    is NodeType.GroundLandingL or NodeType.GroundLedgeL
                    ? (Vector4)Color.green
                    : m_NavigationGrid[navigationGridIndex] is NodeType.GroundLandingR or NodeType.GroundLedgeR
                        ? (Vector4)Color.blue
                        : m_NavigationGrid[navigationGridIndex] is NodeType.GroundLandingMid
                            ? (Vector4)Color.yellow
                            : m_NavigationGrid[navigationGridIndex] is NodeType.GroundLandingLR
                                ? (Vector4)Color.magenta
                                : (Vector4)Color.red;
            else
                colorRef.ValueRW.Value = m_NavigationGrid[navigationGridIndex] switch
                {
                    NodeType.Ground => (Vector4)Color.green,
                    NodeType.GroundLedgeL => (Vector4)new Color(0.05f, 0.6f, 0.55f),
                    NodeType.GroundLedgeR => (Vector4)new Color(0.45f, 0.6f, 0.55f),
                    NodeType.GroundLandingL => (Vector4)new Color(0.05f, 0.6f, 0.05f),
                    NodeType.GroundLandingR => (Vector4)new Color(0.45f, 0.6f, 0.05f),
                    NodeType.Air => (Vector4)new Color(0.27f, 0.23f, 0.36f) ,
                    NodeType.Obstructed => (Vector4)new Color(0.39f, 0.24f, 0.19f),
                    NodeType.JumpDown => (Vector4)Color.yellow,
                    NodeType.JumpUpDown => (Vector4)Color.cyan,
                    _ => (Vector4)Color.clear
                };
        }
        
        new FindPathsJob
        {
            Finder = m_Pathfinder,
            NavGrid = m_NavigationGrid.AsArray(),
            PathNodeLookup = SystemAPI.GetBufferLookup<PathNode>(),
            PathMoveStateLookup = SystemAPI.GetComponentLookup<PathMoveState>(),
        }.Run(SystemAPI.QueryBuilder()
            .WithAll<LocalTransform, PathGoal>()
            .WithDisabledRW<PathNode,PathMoveState>()
            .Build());

        // Move slimes that have a path
        new PathMoveJob
        {
            PathNodeLookup = SystemAPI.GetBufferLookup<PathNode>(),
            PathMoveStateLookup = SystemAPI.GetComponentLookup<PathMoveState>(),
            PathGoalLookup = SystemAPI.GetComponentLookup<PathGoal>(),
            DeltaTime = SystemAPI.Time.DeltaTime,
        }.Run(SystemAPI.QueryBuilder()
            .WithAllRW<PathNode,PathMoveState>()
            .WithAllRW<LocalTransform>()
            .Build());
    }
    
    public void OnStopRunning(ref SystemState state) {}
}

[BurstCompile]
partial struct FindPathsJob : IJobEntity
{
    public Pathfinder Finder;
    [ReadOnly] public NativeArray<NavigationSystem.NodeType> NavGrid;
    [NativeDisableParallelForRestriction] public BufferLookup<PathNode> PathNodeLookup;
    [NativeDisableParallelForRestriction] public ComponentLookup<PathMoveState> PathMoveStateLookup;
    void Execute(Entity entity, in PathGoal goal, in LocalTransform transform)
    {
        var pathNodes = PathNodeLookup[entity];
        var snappedPos = transform.Position.xy * 2 + new float2(1, -1);
        GridNodeIndex startNodeIndex = (int)snappedPos.x + (int)-snappedPos.y * NavigationSystem.navWidth;
        Pathfinder.FindShortestPath(ref Finder, NavGrid, startNodeIndex, goal.nodeIndex, ref pathNodes);
        if (pathNodes.IsEmpty)
        {
            // TODO: need to handle the !found case here. If we just do nothing, we'll immediately search again
            // the next time this job runs, and probably waste a ton of time on failed searches. Maybe some sort of
            // cooldown? Maybe just quietly remove the goal?
        }
        else
        {
            PathNodeLookup.SetBufferEnabled(entity, true);
            // Enable and initialize the move state.
            PathMoveStateLookup.SetComponentEnabled(entity, true);
            PathMoveStateLookup[entity] = new PathMoveState
            {
                From = startNodeIndex,
                To = pathNodes[^1],
                T = 0,
            };
            pathNodes.RemoveAtSwapBack(pathNodes.Length - 1);
        }
    }
    
}

[BurstCompile]
partial struct PathMoveJob : IJobEntity
{
    [NativeDisableParallelForRestriction] public BufferLookup<PathNode> PathNodeLookup;
    [NativeDisableParallelForRestriction] public ComponentLookup<PathMoveState> PathMoveStateLookup;
    [NativeDisableParallelForRestriction] public ComponentLookup<PathGoal> PathGoalLookup;
    public float DeltaTime;
    void Execute(Entity entity, ref LocalTransform transform)
    {
        var moveState = PathMoveStateLookup.GetRefRW(entity);
        // Advance the current move.
        // The current assumption is that a path is always valid once it's been computed; otherwise we would
        // have to handle the case here where the destination node is no longer accessible.
        GridNodeIndex fromNodeIndex = moveState.ValueRO.From;
        GridNodeIndex toNodeIndex = moveState.ValueRO.To;
        
        var toNodePos = new float3((toNodeIndex % NavigationSystem.navWidth)-1, (-toNodeIndex / NavigationSystem.navWidth)+1, -10) * 0.5f;
        moveState.ValueRW.T += DeltaTime;
        if (Hint.Likely(moveState.ValueRO.T < 1.0f))
        {
            // still moving to the current destination node
            var fromNodePos = new float3((fromNodeIndex % NavigationSystem.navWidth)-1, (-fromNodeIndex / NavigationSystem.navWidth)+1, -10) * 0.5f;
            // TODO: something fancier than a simple lerp here
            var fromNodeJumpedPos = new float3((fromNodePos.x+toNodePos.x)/2f, math.max(fromNodePos.y, toNodePos.y)+0.4f, fromNodePos.z);
            var myT = moveState.ValueRO.T * moveState.ValueRO.T * (3.0f - 2.0f * moveState.ValueRO.T);
            transform.Position = QuadraticBezier(fromNodePos, fromNodeJumpedPos, toNodePos, myT);
        }
        else
        {
            // Clamp position to destination node
            transform.Position = toNodePos;
            // Try to grab the next node from the path.
            var pathNodes = PathNodeLookup[entity];
            if (Hint.Unlikely(pathNodes.IsEmpty))
            {
                // we've reached the end of the path.
                PathNodeLookup.SetBufferEnabled(entity, false);
                PathMoveStateLookup.SetComponentEnabled(entity, false);
                PathGoalLookup.SetComponentEnabled(entity, false);
            }
            else
            {
                var nextToNode = pathNodes[^1];
                pathNodes.RemoveAtSwapBack(pathNodes.Length - 1);
                moveState.ValueRW = new PathMoveState {
                    From = toNodeIndex,
                    To = nextToNode,
                    T = moveState.ValueRO.T % 1.0f, // forward any leftover T into the new move
                };
            }
        }
    }
    
    static float3 QuadraticBezier(float3 a, float3 b, float3 c, float t)
    {
        var oneMinusT = 1.0f - t;
        return oneMinusT * oneMinusT * a + 2.0f * oneMinusT * t * b + t * t * c;
    }
}
