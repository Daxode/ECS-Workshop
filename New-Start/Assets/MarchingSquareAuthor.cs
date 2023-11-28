using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


class MarchingSquareAuthor : MonoBehaviour
{
    public MarchSquareSetSprites[] sets;
    public UberOverride spriteTargetPrefab;
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
        
        foreach (var set in authoring.sets)
        {
            if (set.sprites.Length != 16)
                throw new Exception("MarchingSquareAuthor: set must have 16 sprites");
            
            buffer.Add(new MarchSquareSet
            {
                offset0 = set.sprites[0].rect.position,
                offset1 = set.sprites[1].rect.position,
                offset2 = set.sprites[2].rect.position,
                offset3 = set.sprites[3].rect.position,
                offset4 = set.sprites[4].rect.position,
                offset5 = set.sprites[5].rect.position,
                offset6 = set.sprites[6].rect.position,
                offset7 = set.sprites[7].rect.position,
                offset8 = set.sprites[8].rect.position,
                offset9 = set.sprites[9].rect.position,
                offsetA = set.sprites[10].rect.position,
                offsetB = set.sprites[11].rect.position,
                offsetC = set.sprites[12].rect.position,
                offsetD = set.sprites[13].rect.position,
                offsetE = set.sprites[14].rect.position,
                offsetF = set.sprites[15].rect.position,
            });
        }
    }
}

partial struct MarchSquareSystem : ISystem, ISystemStartStop
{
    NativeArray<int> marchSquareLookup;
    static readonly int2 k_MarchSquareLookupSize = new (20, 128);
    
    public void OnCreate(ref SystemState state)
    {
        marchSquareLookup = new NativeArray<int>(k_MarchSquareLookupSize.x * k_MarchSquareLookupSize.y, Allocator.Persistent);
        for (int i = 0; i < marchSquareLookup.Length; i++)
        {
            var x = i % k_MarchSquareLookupSize.x;
            var y = i / k_MarchSquareLookupSize.x;
            var val = noise.cnoise(new float2(x, -y)*0.1f);
            marchSquareLookup[i] = (int)math.round(val * 4f);
            
            Debug.DrawLine(new float3(x, -y, 0), new float3(x, -y, 0) + new float3(0, 0, marchSquareLookup[i]), Color.red, 100f);
        }
        state.RequireForUpdate<MarchSquareData>();
    }

    public void OnDestroy(ref SystemState state)
    {
        marchSquareLookup.Dispose();
    }

    public void OnStartRunning(ref SystemState state)
    {
        var spriteTargetPrefab = SystemAPI.GetSingleton<MarchSquareData>().spriteTargetPrefab;
        var marchSquareSets = SystemAPI.GetSingletonBuffer<MarchSquareSet>();
        
        // loop over 2x2 points
        for (int y = 0; y > -k_MarchSquareLookupSize.y + 1; y--)
        {
            for (int x = 0; x < k_MarchSquareLookupSize.x - 1; x++)
            {
                // get the 4 corners
                var i = x + -y * k_MarchSquareLookupSize.x;
                var valTL = (int) math.saturate(marchSquareLookup[i]);
                var valTR = (int) math.saturate(marchSquareLookup[i + 1]);
                var valBL = (int) math.saturate(marchSquareLookup[i + k_MarchSquareLookupSize.x]);
                var valBR = (int) math.saturate(marchSquareLookup[i + k_MarchSquareLookupSize.x + 1]);
                
                // build a 4-bit value from the 4 corners
                var valCombined = valBL | (valBR << 1) | (valTR << 2) | (valTL << 3);
                var sprite = marchSquareSets[0].GetOffset(valCombined);
                
                // spawn a sprite with the correct offset
                var spriteTarget = state.EntityManager.Instantiate(spriteTargetPrefab);
                var originalOffset = SystemAPI.GetComponent<MaterialOverrideOffsetXYScaleZW>(spriteTargetPrefab);
                SystemAPI.SetComponent(spriteTarget, LocalTransform.FromPosition(new float3(x, y, 0)));
                SystemAPI.SetComponent(spriteTarget, new MaterialOverrideOffsetXYScaleZW { Value = new float4(sprite*originalOffset.Value.zw, 32*originalOffset.Value.zw) });
            }
        }
    }

    public void OnStopRunning(ref SystemState state) {}
}
