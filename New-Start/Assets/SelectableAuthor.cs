using System;
using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class SelectableAuthor : MonoBehaviour
{
    [SerializeField] AnimatedSpriteAuthor drawnOutline;
    
    class Baker : Baker<SelectableAuthor>
    {
        public override void Bake(SelectableAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(entity, new Selectable
            {
                outlineEntity = authoring.drawnOutline ? GetEntity(authoring.drawnOutline, TransformUsageFlags.Dynamic) : Entity.Null
            });
            SetComponentEnabled<Selectable>(entity, false);
            
            AddComponent<WalkState>(entity);
        }
    }
}

struct WalkState : IComponentData
{
    public float2 target;
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

partial struct NavigationSystem : ISystem, ISystemStartStop
{
    NativeList<NodeType> m_NavigationGrid;

    public enum NodeType
    {
        Air,
        Ground,
        Obstructed,
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

    public void OnUpdate(ref SystemState state)
    {
        if (Input.GetKeyDown(KeyCode.G))
            showDebugGrid = !showDebugGrid;
        
        var caveSingleton = SystemAPI.GetSingleton<CaveGridSystem.Singleton>();
        var caveTileHeight = caveSingleton.GetCaveTileHeight();
        var corners = caveSingleton.CaveGrid.AsArray();
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
                            NodeType.Ground, NodeType.Ground, NodeType.Air,
                            NodeType.Obstructed, NodeType.Air, NodeType.Air);
                        break;
                    case 2: // 0010
                        m_NavigationGrid.SetNavigation(ref navIndexes,
                            NodeType.Air, NodeType.Air, NodeType.Air,
                            NodeType.Air, NodeType.Ground, NodeType.Ground,
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

        // Find green, and if blue up and down, set all blues to jump down until first non blue
        for (int navIndex = 0; navIndex < m_NavigationGrid.Length; navIndex++)
        {
            if (m_NavigationGrid[navIndex] == NodeType.Ground)
            {
                var neighborU = navIndex - navWidth;
                var current = navIndex;
                var neighborD = navIndex + navWidth;
                if (neighborU < 0 || neighborD >= m_NavigationGrid.Length) continue;

                while (m_NavigationGrid[neighborU] is NodeType.Air or NodeType.JumpDown or NodeType.Ground
                       && m_NavigationGrid[neighborD] is NodeType.Air or NodeType.JumpDown)
                {
                    m_NavigationGrid[neighborD] = NodeType.JumpDown;
                    
                    neighborU = current;
                    current = neighborD;
                    neighborD += navWidth;
                    if (neighborD >= m_NavigationGrid.Length) break;
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
                if (m_NavigationGrid[neighborU] == NodeType.Ground && m_NavigationGrid[neighborD] == NodeType.Ground)
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
            
            colorRef.ValueRW.Value = m_NavigationGrid[navigationGridIndex] switch
            {
                NodeType.Ground => (Vector4)Color.green,
                NodeType.Air => (Vector4)new Color(0.27f, 0.23f, 0.36f) ,
                NodeType.Obstructed => (Vector4)new Color(0.39f, 0.24f, 0.19f),
                NodeType.JumpDown => (Vector4)Color.yellow,
                NodeType.JumpUpDown => (Vector4)Color.cyan,
                _ => (Vector4)Color.clear
            };
        }
        
        foreach (var (ltRef, walkState) in SystemAPI
                     .Query<RefRW<LocalTransform>, WalkState>())
        {
            ref var ltw = ref ltRef.ValueRW;
            var target = walkState.target;
            var targetPos = new float3(target.x, target.y, -3);
            var dir = math.normalize(targetPos - ltw.Position);
            var speed = 5f;
            var distance = math.distance(targetPos, ltw.Position);
            if (distance > 0.5f) 
                ltw.Position += dir * speed * SystemAPI.Time.DeltaTime;
        }
    }
    
    public void OnStopRunning(ref SystemState state) {}
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
    /// <param name="navigationGrid">The current grid state.</param>
    /// <param name="start">index of the starting point on the grid.</param>
    /// <param name="end">index of the destination on the grid.</param>
    /// <param name="outPath">Contains the sequence of nodes to visit to reach <paramref name="end"/> from
    /// <paramref name="start"/>, including these two nodes themselves. If no path is found, this list will be
    /// empty.</param>
    /// <returns>True if a path from start to end was found, or false if end is not reachable from start.</returns>
    public bool FindShortestPath(in NativeArray<NavigationSystem.NodeType> navigationGrid, int start, int end, ref NativeList<int> outPath)
    {
        outPath.Clear();
        
        // Handle the degenerate cases:
        if (navigationGrid[start] != NavigationSystem.NodeType.Air ||
            navigationGrid[end] != NavigationSystem.NodeType.Air)
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
            if (x > 0 && navigationGrid[neighborL] == NavigationSystem.NodeType.Air)
                ProcessNeighbor(currentNode, neighborL, currentLength + MOVE_COST_LEFTRIGHT);
            if (x < CaveGridSystem.Singleton.CaveGridWidth-1 && navigationGrid[neighborR] == NavigationSystem.NodeType.Air)
                ProcessNeighbor(currentNode, neighborR, currentLength + MOVE_COST_LEFTRIGHT);
            if (y > 0 && navigationGrid[neighborU] == NavigationSystem.NodeType.Air)
                ProcessNeighbor(currentNode, neighborU, currentLength + MOVE_COST_UP);
            if (y < CaveGridSystem.Singleton.CaveGridWidth-1 && navigationGrid[neighborD] == NavigationSystem.NodeType.Air)
                ProcessNeighbor(currentNode, neighborD, currentLength + MOVE_COST_DOWN);
        }
        // If the candidates array empties without visiting the end node, it means the end node isn't reachable
        // from the start node.
        return false;
    }
}

partial struct SelectableSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (data, selectState) in SystemAPI
                     .Query<Selectable, EnabledRefRO<Selectable>>()
                     .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
        {
            SystemAPI.SetComponent(data.outlineEntity, selectState.ValueRO ? LocalTransform.Identity : default);
        }
    }
}