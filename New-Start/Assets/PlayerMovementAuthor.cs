using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
// Authoring Data
public class PlayerMovementAuthor : MonoBehaviour {
    [SerializeField] InputAction directionalInput;
    [SerializeField] float speed;
    [SerializeField] Transform model;
    
    public class PlayerInputManaged : IComponentData {
        public InputAction directionalInput;
        public float speed;
        public Entity modelEntity;
    }
    
    class Baker : Baker<PlayerMovementAuthor> {
        public override void Bake(PlayerMovementAuthor authoring) {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponentObject(entity, new PlayerInputManaged {
                directionalInput = authoring.directionalInput,
                speed = authoring.speed,
                modelEntity = authoring.model ? GetEntity(authoring.model, TransformUsageFlags.Dynamic) : Entity.Null
            });

            // lock to xz plane
            var jointEntity = CreateAdditionalEntity(TransformUsageFlags.None);
            AddComponent(jointEntity, PhysicsJoint.CreateLimitedDOF(RigidTransform.identity,
                    false, true));
            AddComponent(jointEntity, new PhysicsConstrainedBodyPair(entity, Entity.Null, false));
            AddComponent<PhysicsWorldIndex>(jointEntity);
        }
    }
}

partial struct MyInitSystem : ISystem, ISystemStartStop {
    static readonly int k_Blend = Animator.StringToHash("Blend");

    public void OnCreate(ref SystemState state) {
        state.RequireForUpdate<PlayerMovementAuthor.PlayerInputManaged>();
    }

    public void OnStartRunning(ref SystemState state) {
        foreach (var playerInputManaged in SystemAPI.Query<PlayerMovementAuthor.PlayerInputManaged>()) {
            playerInputManaged.directionalInput.Enable();
        }
    }

    public void OnStopRunning(ref SystemState state)
    {
        foreach (var playerInputManaged in SystemAPI.Query<PlayerMovementAuthor.PlayerInputManaged>()) {
            playerInputManaged.directionalInput.Disable();
        }
    }

    public void OnUpdate(ref SystemState state) {
        var timeData = SystemAPI.Time;
        foreach (var (playerInputManaged, velocityRef) in SystemAPI.Query<PlayerMovementAuthor.PlayerInputManaged, RefRW<PhysicsVelocity>>()) {
            var input = playerInputManaged.directionalInput.ReadValue<Vector2>();
            var preserveY = velocityRef.ValueRO.Linear.y;
            velocityRef.ValueRW.Linear = new float3(input.x, 0, input.y) * playerInputManaged.speed * timeData.DeltaTime;
            velocityRef.ValueRW.Linear.y = preserveY;

            // check if model is Animator and set the Animator's speed
            if (SystemAPI.ManagedAPI.HasComponent<Animator>(playerInputManaged.modelEntity)) {
                var animator = SystemAPI.ManagedAPI.GetComponent<Animator>(playerInputManaged.modelEntity);
                animator.SetFloat(k_Blend, math.length(velocityRef.ValueRO.Linear));
            }
        }
    }
}

partial struct RotateModelToPhysicsVel : ISystem {
    public void OnUpdate(ref SystemState state) {
        // loop through all entities with a PhysicsVelocity and PlayerInputManaged
        foreach (var (velocity, playerInputManaged) in SystemAPI.Query<PhysicsVelocity, PlayerMovementAuthor.PlayerInputManaged>()) {
            if (math.lengthsq(velocity.Linear) < 0.01f) continue;
            
            var modelEntity = playerInputManaged.modelEntity;
            var modelLT = SystemAPI.GetComponent<LocalTransform>(modelEntity);
            modelLT.Rotation = math.mul(modelLT.Rotation,
                quaternion.RotateY(Vector2.SignedAngle(velocity.Linear.xz, modelLT.Forward().xz)*0.001f));
            SystemAPI.SetComponent(modelEntity, modelLT);
        }
    }
}