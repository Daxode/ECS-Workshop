using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class MarchingSquareTileLayerAuthor : MonoBehaviour
{
    public MarchingSquareTagType tagType;
    public enum MarchingSquareTagType
    {
        Carver,
        MatA,
        MatB,
    }
}

class MarchingSquareTileLayerBaker : Baker<MarchingSquareTileLayerAuthor>
{
    public override void Bake(MarchingSquareTileLayerAuthor authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Renderable);
        AddComponent(entity, new MaterialOverrideCornerStrength{ Value = 1 });;
        AddComponent(entity, new MaterialOverrideOffsetXYScaleZW { Value = new float4(
            0, // offset
            DependsOn(GetComponentInParent<MarchingSquareTileAuthor>().spriteTextureSheet).texelSize * 32 // scale
        )});

        switch (authoring.tagType)
        {
            case MarchingSquareTileLayerAuthor.MarchingSquareTagType.Carver:
                AddComponent(entity, new MarchingSquareTileCarverTag());
                break;
            case MarchingSquareTileLayerAuthor.MarchingSquareTagType.MatA:
                AddComponent(entity, new MarchingSquareTileMatATag());
                break;
            case MarchingSquareTileLayerAuthor.MarchingSquareTagType.MatB:
                AddComponent(entity, new MarchingSquareTileMatBTag());
                break;
        }
    }
}


[MaterialProperty("_CornerStrength")]
public struct MaterialOverrideCornerStrength : IComponentData
{
    public float4 Value;
}

[MaterialProperty("_OffsetXYScaleZW")]
public struct MaterialOverrideOffsetXYScaleZW : IComponentData
{
    public float4 Value;
}