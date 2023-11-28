using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


class MarchingSquareAuthor : MonoBehaviour
{
    public MarchSquareSetSprites[] sets;
    public MarchingSquareTile spriteTargetPrefab;
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
            var water = math.select(0, 3, noise.cnoise(new float2(x, -y)*0.1f)>0.4f);
            var ore = math.select(0, 2, noise.cnoise(new float3(x, -y, 0.2f)*0.1f)>0.2f);
            var air = math.select(0, 1, noise.cnoise(new float3(x, -y, 0.4f)*0.1f)>0.3f);
            marchSquareLookup[i] = math.min(math.max(water, ore), air);
            
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
        
        // loop over 2x2 points
        for (int y = 0; y > -k_MarchSquareLookupSize.y + 1; y--)
        {
            for (int x = 0; x < k_MarchSquareLookupSize.x - 1; x++)
            {
                // spawn a sprite with the correct offset
                var spriteTarget = state.EntityManager.Instantiate(spriteTargetPrefab);
                SystemAPI.SetComponent(spriteTarget, LocalTransform.FromPosition(new float3(x, y, 0)));
            }
        }
    }

    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var marchSquareSets = SystemAPI.GetSingletonBuffer<MarchSquareSet>();
        foreach (var (lt, offsetXYScaleZwRef) in SystemAPI.Query<LocalToWorld, RefRW<MaterialOverrideOffsetXYScaleZW>>().WithAll<MarchingSquareTileCarverTag>())
        {
            var corners = GetCornerValues((int2)lt.Position.xy);

            // build a 4-bit value from the 4 corners
            corners = 1 - (int4) math.saturate(corners);
            var valCombined = corners.x | (corners.y << 1) | (corners.z << 2) | (corners.w << 3);
            offsetXYScaleZwRef.ValueRW.Value.xy = marchSquareSets[0].GetOffset(valCombined);
        }
        
        foreach (var (lt, offsetXYScaleZwRef, cornerStrengthRef) in SystemAPI.Query<LocalToWorld, RefRW<MaterialOverrideOffsetXYScaleZW>, RefRW<MaterialOverrideCornerStrength>>().WithAll<MarchingSquareTileMatATag>())
        {
            var corners = GetCornerValues((int2)lt.Position.xy);
            var highestCorners = SortTheFourNumbers(corners);
            cornerStrengthRef.ValueRW.Value = (float4) (corners == highestCorners.w);
            
            // build a 4-bit value from the 4 corners
            corners = 1 - (int4) math.saturate(corners);
            var valCombined = corners.x | (corners.y << 1) | (corners.z << 2) | (corners.w << 3);
            offsetXYScaleZwRef.ValueRW.Value.xy = marchSquareSets[math.clamp(highestCorners.w-1, 0, marchSquareSets.Length-1)].GetOffset(valCombined);
            
        }
        
        foreach (var (lt, offsetXYScaleZwRef, cornerStrengthRef) in SystemAPI.Query<LocalToWorld, RefRW<MaterialOverrideOffsetXYScaleZW>, RefRW<MaterialOverrideCornerStrength>>().WithAll<MarchingSquareTileMatBTag>())
        {
            var corners = GetCornerValues((int2)lt.Position.xy);
            var highestCorners = SortTheFourNumbers(corners);
            
            var validSecond = 0;
            if (highestCorners.w != highestCorners.z)
                validSecond = highestCorners.z;
            else if (highestCorners.w != highestCorners.y)
                validSecond = highestCorners.y;
            else if (highestCorners.w != highestCorners.x)
                validSecond = highestCorners.x;
            
            if (validSecond == 0)
                cornerStrengthRef.ValueRW.Value = 0;
            else
                cornerStrengthRef.ValueRW.Value = (float4) (corners == validSecond);
            
            // build a 4-bit value from the 4 corners
            corners = 1 - (int4) math.saturate(corners);
            var valCombined = corners.x | (corners.y << 1) | (corners.z << 2) | (corners.w << 3);
            offsetXYScaleZwRef.ValueRW.Value.xy = marchSquareSets[math.clamp(validSecond-1, 0, marchSquareSets.Length-1)].GetOffset(valCombined);
        }

        if (Input.GetKey(KeyCode.Mouse0) || Input.GetKey(KeyCode.Mouse1))
        {
            var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var mousePosInt = (int2) (math.round(((float3)mousePos).xy+new float2(0.5f, -0.5f)));
            var i = mousePosInt.x + -mousePosInt.y * k_MarchSquareLookupSize.x;
            if (Input.GetKey(KeyCode.LeftShift))
                marchSquareLookup[i] = math.select(2, 3, Input.GetKey(KeyCode.Mouse0));
            else
                marchSquareLookup[i] = math.select(0, 1, Input.GetKey(KeyCode.Mouse0));
        }
        
        // camera y up/down from scroll
        if (Input.mouseScrollDelta != Vector2.zero)
        {
            var camera = Camera.main;
            camera.transform.position += new Vector3(0, Input.mouseScrollDelta.y, 0);
        }
    }

    static int4 SortTheFourNumbers(int4 val)
    {
        var lowhigh1 = new int2();
        var lowhigh2 = new int2();
        var lowestmiddle1 = new int2();
        var middle2highest = new int2();
        
        lowhigh1 = math.select(val.yx, val.xy, val.x < val.y);
        lowhigh2 = math.select(val.wz, val.zw, val.z < val.w);
        lowestmiddle1 = math.select(new int2(lowhigh2.x, lowhigh1.x), new int2(lowhigh1.x, lowhigh2.x), lowhigh1.x < lowhigh2.x);
        middle2highest = math.select(new int2(lowhigh2.y, lowhigh1.y), new int2(lowhigh1.y, lowhigh2.y), lowhigh1.y < lowhigh2.y);

        return math.select(
            new int4(lowestmiddle1.x,middle2highest.x, lowestmiddle1.y, middle2highest.y), 
            new int4(lowestmiddle1, middle2highest), 
            lowestmiddle1.y < middle2highest.x);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int4 GetCornerValues(int2 coord)
    {
        // assert that y is negative or ground level. As we're going from top to bottom, y should be negative
        if (coord.y > 0) throw new Exception("y must be negative");

        // get the 4 corners
        var i = coord.x + -coord.y * k_MarchSquareLookupSize.x;
        return new int4(
            marchSquareLookup[i + k_MarchSquareLookupSize.x], // Bottom Left
            marchSquareLookup[i + k_MarchSquareLookupSize.x + 1], // Bottom Right
            marchSquareLookup[i + 1], // Top Right
            marchSquareLookup[i] // Top Left
        );
    }

    public void OnStopRunning(ref SystemState state) {}
}
