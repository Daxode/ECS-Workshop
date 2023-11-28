using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class UberOverride : MonoBehaviour
{
    public float4 CornerStrength = 1f;
    public float4 OffsetXYScaleZW = new (0,0, 1, 1);
    public Texture2D defaultShader;
}

class SpriteBaker : Baker<UberOverride>
{
    public override void Bake(UberOverride authoring)
    {
        
        var entity = GetEntity(TransformUsageFlags.Renderable);
        AddComponent(entity, new MaterialOverrideCornerStrength{ Value = authoring.CornerStrength });;
        AddComponent(entity, new MaterialOverrideOffsetXYScaleZW { Value = new float4(
            0, // offset
            DependsOn(authoring.defaultShader).texelSize // scale
        )});
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