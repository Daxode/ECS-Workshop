using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class MarchingSquareTileLayer : MonoBehaviour
{
    public MarchingSquareTagType tagType;
    public enum MarchingSquareTagType
    {
        Carver,
        MatA,
        MatB,
    }
}

class MarchingSquareTileLayerBaker : Baker<MarchingSquareTileLayer>
{
    public override void Bake(MarchingSquareTileLayer authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Renderable);
        AddComponent(entity, new MaterialOverrideCornerStrength{ Value = 1 });;
        AddComponent(entity, new MaterialOverrideOffsetXYScaleZW { Value = new float4(
            0, // offset
            DependsOn(GetComponentInParent<MarchingSquareTile>().spriteTextureSheet).texelSize * 32 // scale
        )});

        switch (authoring.tagType)
        {
            case MarchingSquareTileLayer.MarchingSquareTagType.Carver:
                AddComponent(entity, new MarchingSquareTileCarverTag());
                break;
            case MarchingSquareTileLayer.MarchingSquareTagType.MatA:
                AddComponent(entity, new MarchingSquareTileMatATag());
                break;
            case MarchingSquareTileLayer.MarchingSquareTagType.MatB:
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

struct MarchingSquareTileCarverTag : IComponentData {}
struct MarchingSquareTileMatATag : IComponentData {}
struct MarchingSquareTileMatBTag : IComponentData {}