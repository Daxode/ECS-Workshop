using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

// Based on the EntitiesSample 'BakingDependencies' found here:
// https://github.com/Unity-Technologies/EntityComponentSystemSamples/tree/master/EntitiesSamples/Assets/Baking/BakingDependencies
[RequireComponent(typeof(BoxCollider))]
public class GrassFieldAuthoring : MonoBehaviour
{
    [SerializeField] GameObject grassPrefab;
    [SerializeField] int grassCount = 100;
    [SerializeField] uint seed;
    class Baker : Baker<GrassFieldAuthoring>
    {
        public override void Bake(GrassFieldAuthoring authoring)
        {
            var boxCollider = GetComponent<BoxCollider>(authoring);
            var meshRenderer = GetComponentInChildren<MeshRenderer>(authoring.grassPrefab);
            var meshFilter = GetComponent<MeshFilter>(meshRenderer);
            DependsOn(authoring.transform);
            
            // return if any null
            if (boxCollider == null || meshRenderer == null || meshFilter == null)
                return;
            
            var mainEntity = GetEntity(TransformUsageFlags.None);
            var entities = AddBuffer<GrassEntity>(mainEntity);
            var size = ((float3)boxCollider.size).xz * ((float3)authoring.transform.localScale).xz * 0.5f;
            var random = Unity.Mathematics.Random.CreateFromIndex(authoring.seed);
            for (var i = 0; i < authoring.grassCount; i++)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.ManualOverride);
                var worldPosition = new Vector3(
                    random.NextFloat(-size.x, size.x),
                    0,
                    random.NextFloat(-size.y, size.y)
                ) + boxCollider.center + authoring.transform.position;
                AddComponent(entity, new LocalToWorld{Value = float4x4.Translate(worldPosition)});
                entities.Add(entity);
            }
            
            AddComponentObject(mainEntity, new MeshArrayBakingType
            {
                meshArray = new RenderMeshArray(new[] { meshRenderer.sharedMaterial }, new[] { meshFilter.sharedMesh })
            });
            AddComponent<BakingOnlyEntity>(mainEntity);
        }
    }
}

[BakingType]
public class MeshArrayBakingType : IComponentData
{
    public RenderMeshArray meshArray;
}

[BakingType]
public struct GrassEntity : IBufferElementData
{
    Entity m_Value;
    public static implicit operator Entity(GrassEntity e) => e.m_Value;
    public static implicit operator GrassEntity(Entity e) => new() { m_Value = e };
}

[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
partial struct GrassFieldBakingSystem : ISystem
{
    EntityQuery m_GrassEntitiesQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        m_GrassEntitiesQuery = SystemAPI.QueryBuilder().WithAll<GrassEntity, MeshArrayBakingType>().Build();
        m_GrassEntitiesQuery.SetChangedVersionFilter(ComponentType.ReadOnly<GrassEntity>());
        state.RequireForUpdate(m_GrassEntitiesQuery);
    }

    public void OnUpdate(ref SystemState state)
    {
        var renderMeshDescription = new RenderMeshDescription(UnityEngine.Rendering.ShadowCastingMode.Off);
        var materialMeshInfo = MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0);
        foreach (var entity in m_GrassEntitiesQuery.ToEntityArray(state.WorldUpdateAllocator))
        {
            var meshArray = SystemAPI.ManagedAPI.GetComponent<MeshArrayBakingType>(entity).meshArray;
            var bakingEntities = SystemAPI.GetBuffer<GrassEntity>(entity).Reinterpret<Entity>()
                .ToNativeArray(state.WorldUpdateAllocator); // as structural change invalidates the buffer
            foreach (var bakingEntity in bakingEntities)
                RenderMeshUtility.AddComponents(bakingEntity, state.EntityManager, renderMeshDescription,
                    meshArray, materialMeshInfo);
        }
    }
}
