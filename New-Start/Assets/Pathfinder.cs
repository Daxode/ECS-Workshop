using System;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Wrapper to distinguish an integer node index in the grid from a raw integer.
/// </summary>
public struct GridNode : IComparable<GridNode>
{
    public int Index;
    public static implicit operator int(GridNode n) => n.Index;
    public static implicit operator GridNode(int i) => new GridNode { Index = i };
    public int CompareTo(GridNode other)
    {
        return Index.CompareTo(other.Index);
    }
}

/// <summary>
/// 
/// </summary>
public struct PathGoal : IComponentData, IEnableableComponent
{
    public GridNode Node;
}

public struct PathMoveState : IComponentData, IEnableableComponent
{
    public GridNode From, To;
    public float T; // 0..1
}

/// <summary>
/// The nodes comprising a path to the target node stored in the <see cref="PathGoal"/> component. 
/// </summary>
[InternalBufferCapacity(0)]
public struct PathNode : IBufferElementData, IEnableableComponent
{
    public GridNode Node;
    public static implicit operator PathNode(GridNode n) => new PathNode { Node = n };
    public static implicit operator GridNode(PathNode n) => n.Node;
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
    private NativeArray<GridNode> prevNodes; // Index of the previous node in the shortest path from the start to each node. undefined for unvisited nodes
    private NativeList<GridNode> candidateNodes;
    private const int MOVE_COST_LEFTRIGHT = 2;
    private const int MOVE_COST_UP = 3;
    private const int MOVE_COST_DOWN = 1;

    public Pathfinder(int nodeCount, Allocator allocator)
    {
        nodeStates = new NativeArray<NodeState>(nodeCount, allocator, NativeArrayOptions.ClearMemory);
        pathCosts = new NativeArray<int>(nodeCount, allocator, NativeArrayOptions.UninitializedMemory);
        pathNodeCounts = new NativeArray<int>(nodeCount, allocator, NativeArrayOptions.UninitializedMemory);
        prevNodes = new NativeArray<GridNode>(nodeCount, allocator, NativeArrayOptions.UninitializedMemory);
        candidateNodes = new NativeList<GridNode>(nodeCount, allocator);
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

    void ProcessNeighbor(GridNode currentNode, GridNode neighbor, int newCost)
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
    public static void FindShortestPath(ref Pathfinder pathfinder,in NativeArray<CaveMaterialType> caveGrid, GridNode start, GridNode end,
        ref DynamicBuffer<PathNode> outPath)
    {
        pathfinder.FindShortestPath(caveGrid, start, end, ref outPath);
    }

    void FindShortestPath(in NativeArray<CaveMaterialType> caveGrid, GridNode start, GridNode end, ref DynamicBuffer<PathNode> outPath)
    {
        outPath.Clear();
        
        // Handle the degenerate cases:
        if (caveGrid[start] != CaveMaterialType.Air ||
            caveGrid[end] != CaveMaterialType.Air)
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
                GridNode n = end;
                do
                {
                    outPath.Add(n);
                    n = prevNodes[n];
                } while (n != start);
                return;
            }
            GridNode currentNode = candidateNodes[nextCandidateIndex];
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
            GridNode neighborL = currentNode - 1;
            GridNode neighborR = currentNode + 1;
            GridNode neighborU = currentNode - CaveGridSystem.Singleton.CaveGridWidth;
            GridNode neighborD = currentNode + CaveGridSystem.Singleton.CaveGridWidth;
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
        return;
    }
}

[BurstCompile]
public partial struct SlimeMoveSystem : ISystem
{
    private Pathfinder _pathfinder;
    private EntityQuery _needsPathQuery;
    private EntityQuery _hasPathQuery;
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CaveGridSystem.Singleton>();
        _needsPathQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<LocalTransform, PathGoal>()
            .WithDisabledRW<PathNode,PathMoveState>()
            .Build(ref state);
        _hasPathQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAllRW<PathNode,PathMoveState>()
            .WithAllRW<LocalTransform>()
            .Build(ref state);
    }

    public void OnDestroy(ref SystemState state)
    {
        _pathfinder.Dispose();
    }

    [BurstCompile]
    partial struct FindPathsJob : IJobEntity
    {
        public Pathfinder Finder;
        [ReadOnly] public NativeArray<CaveMaterialType> CaveGrid; 
        public int CaveGridWidth;
        [NativeDisableParallelForRestriction] public BufferLookup<PathNode> PathNodeLookup;
        [NativeDisableParallelForRestriction] public ComponentLookup<PathMoveState> PathMoveStateLookup;
        void Execute(Entity entity, in PathGoal goal, in LocalTransform transform)
        {
            var pathNodes = PathNodeLookup[entity];
            GridNode startNode = (int)transform.Position.x - (int)transform.Position.y * CaveGridWidth;
            Pathfinder.FindShortestPath(ref Finder, CaveGrid, startNode, goal.Node, ref pathNodes);
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
                    From = startNode,
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
        public int CaveGridWidth;
        public float DeltaTime;
        void Execute(Entity entity, ref LocalTransform transform)
        {
            var moveState = PathMoveStateLookup.GetRefRW(entity);
            // Advance the current move.
            // The current assumption is that a path is always valid once it's been computed; otherwise we would
            // have to handle the case here where the destination node is no longer accessible.
            GridNode fromNode = moveState.ValueRO.From;
            GridNode toNode = moveState.ValueRO.To;
            var toNodePos = new float3(toNode % CaveGridWidth, -toNode / CaveGridWidth, 0);
            moveState.ValueRW.T += DeltaTime;
            if (Hint.Likely(moveState.ValueRO.T < 1.0f))
            {
                // still moving to the current destination node
                var fromNodePos = new float3(fromNode % CaveGridWidth, -fromNode / CaveGridWidth, 0);
                // TODO: something fancier than a simple lerp here
                transform.Position = math.lerp(fromNodePos, toNodePos, moveState.ValueRO.T);
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
                        From = toNode,
                        To = nextToNode,
                        T = moveState.ValueRO.T % 1.0f, // forward any leftover T into the new move
                    };
                }
            }
        }
    }

    public void OnUpdate(ref SystemState state)
    {
        var caveGrid = SystemAPI.GetSingletonRW<CaveGridSystem.Singleton>().ValueRW.CaveGrid.AsArray();
        if (Hint.Unlikely(!_pathfinder.IsCreated))
            _pathfinder = new Pathfinder(caveGrid.Length, Allocator.Persistent);
        // For each slime with a goal but no path, compute a path.
        // TODO: can't use IFE here because SystemAPI.Query.WithDisabled<T>() doesn't work if T is an IBufferElementData.
        // TODO: convert to a parallel job, but this requires having a separate pathfinder per thread.
        state.CompleteDependency();
        var pathJob = new FindPathsJob
        {
            Finder = _pathfinder,
            CaveGrid = caveGrid,
            CaveGridWidth = CaveGridSystem.Singleton.CaveGridWidth,
            PathNodeLookup = SystemAPI.GetBufferLookup<PathNode>(false),
            PathMoveStateLookup = SystemAPI.GetComponentLookup<PathMoveState>(false),
        };
        pathJob.RunByRef(_needsPathQuery);

        // Move slimes that have a path
        var moveJob = new PathMoveJob
        {
            PathNodeLookup = SystemAPI.GetBufferLookup<PathNode>(false),
            PathMoveStateLookup = SystemAPI.GetComponentLookup<PathMoveState>(false),
            PathGoalLookup = SystemAPI.GetComponentLookup<PathGoal>(false),
            CaveGridWidth = CaveGridSystem.Singleton.CaveGridWidth,
            DeltaTime = SystemAPI.Time.DeltaTime,
        };
        moveJob.Run(_hasPathQuery);
        state.Dependency = default;
    }
}