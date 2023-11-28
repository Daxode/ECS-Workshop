using System;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class Rock : RuleTile<Rock.Neighbor>
{
    public TileBase TileToMatch;
    
    public class Neighbor : TilingRuleOutput.Neighbor {
        public const int MatchesTile = 3;
        public const int MatchesTileAndNotThis = 4;
        public const int HasTile = 5;
    }

    public override bool RuleMatch(int neighbor, TileBase tile) {
        switch (neighbor) {
            case Neighbor.MatchesTile: return tile == TileToMatch;
            case Neighbor.MatchesTileAndNotThis: return tile == TileToMatch && tile != this;
            case Neighbor.HasTile: return tile != null;
        }
        
        return base.RuleMatch(neighbor, tile);
    }
}