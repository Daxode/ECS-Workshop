using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[RequireComponent(typeof(StillRotationModelAuthor))]
public class FollowPlayer : MonoBehaviour
{
    [SerializeField] float speed;
    private class Baker : Baker<FollowPlayer>
    {
        public override void Bake(FollowPlayer authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new FollowPlayerData
            {
                speed = authoring.speed
            });
        }
    }

    internal struct FollowPlayerData : IComponentData
    {
        public Entity playerEntity;
        public float speed;
    }
}

partial struct FollowPlayerSystem : ISystem
{
    static readonly int k_Blend = Animator.StringToHash("Blend");

    public void OnUpdate(ref SystemState state)
    {
        foreach (var (followPlayerDataRef, ltRef, velRef, rotateTowardsData, model) in SystemAPI.Query<
                     RefRW<FollowPlayer.FollowPlayerData>, RefRO<LocalTransform>, 
                     RefRW<PhysicsVelocity>, RefRW<RotateTowardsData>, ModelForEntity>())
        {
            var followPlayerData = followPlayerDataRef.ValueRO;
            if (followPlayerData.playerEntity == Entity.Null)
                followPlayerDataRef.ValueRW.playerEntity = SystemAPI.ManagedAPI.GetSingletonEntity<PlayerMovementAuthor.PlayerInputManaged>();
            else {
                var playerPosition = SystemAPI.GetComponent<LocalTransform>(followPlayerData.playerEntity).Position;
                var direction = playerPosition - ltRef.ValueRO.Position;
                var dirNormalized = math.normalize(direction);
                var velocity = dirNormalized * followPlayerData.speed;
                
                // slow down when close
                var distance = math.length(direction);
                velocity *= math.smoothstep(4, 6, distance);
                velRef.ValueRW.Linear = velocity;
                
                // set animator
                if (SystemAPI.ManagedAPI.HasComponent<Animator>(model.modelEntity)) {
                    var animator = SystemAPI.ManagedAPI.GetComponent<Animator>(model.modelEntity);
                    animator.SetFloat(k_Blend, math.length(velocity));
                }
                
                // rotate towards
                rotateTowardsData.ValueRW.direction = dirNormalized;
            }
        }
    }
}