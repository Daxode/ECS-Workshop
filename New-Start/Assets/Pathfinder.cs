using System;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
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
    /// <param name="outPath">Contains the sequence of nodes to visit to reach <paramref name="end"/> from
    /// <paramref name="start"/>, including these two nodes themselves. If no path is found, this list will be
    /// empty.</param>
    /// <returns>True if a path from start to end was found, or false if end is not reachable from start.</returns>
    [BurstCompile]
    public static bool FindShortestPath(ref Pathfinder pathfinder,in NativeArray<CaveMaterialType> caveGrid, GridNode start, GridNode end,
        ref NativeList<GridNode> outPath)
    {
        return pathfinder.FindShortestPath(caveGrid, start, end, ref outPath);
    }

    bool FindShortestPath(in NativeArray<CaveMaterialType> caveGrid, GridNode start, GridNode end, ref NativeList<GridNode> outPath)
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
                GridNode n = end;
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

