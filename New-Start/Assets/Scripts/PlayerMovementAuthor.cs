using System;
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
    [SerializeField] float rotateSpeed;
    
    public class PlayerInputManaged : IComponentData {
        public InputAction directionalInput;
        public float speed;
    }
    
    class Baker : Baker<PlayerMovementAuthor> {
        public override void Bake(PlayerMovementAuthor authoring) {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponentObject(entity, new PlayerInputManaged {
                directionalInput = authoring.directionalInput,
                speed = authoring.speed,
            });
            
            // Make sure the model spins to face the direction of movement
            AddComponent(entity, new ModelForEntity {
                modelEntity = authoring.model ? GetEntity(authoring.model, TransformUsageFlags.Dynamic) : Entity.Null
            });
            AddComponent(entity, new RotateTowardsData {
                speed = authoring.rotateSpeed
            });
            
            // lock to xz plane
            var jointEntity = CreateAdditionalEntity(TransformUsageFlags.None);
            AddComponent(jointEntity, PhysicsJoint.CreateLimitedDOF(RigidTransform.identity,
                new bool3(false,true,false), true));
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
        foreach (var (playerInputManaged, velocityRef, model, towardsDataRef) in SystemAPI.Query<
                     PlayerMovementAuthor.PlayerInputManaged, RefRW<PhysicsVelocity>, 
                     ModelForEntity, RefRW<RotateTowardsData>>()) {
            var input = playerInputManaged.directionalInput.ReadValue<Vector2>();
            var preserveY = velocityRef.ValueRO.Linear.y;
            velocityRef.ValueRW.Linear = new float3(input.x, 0, input.y) * playerInputManaged.speed;
            velocityRef.ValueRW.Linear.y = preserveY;
            towardsDataRef.ValueRW.direction = velocityRef.ValueRO.Linear;

            // check if model is Animator and set the Animator's speed
            if (SystemAPI.ManagedAPI.HasComponent<Animator>(model.modelEntity)) {
                var animator = SystemAPI.ManagedAPI.GetComponent<Animator>(model.modelEntity);
                animator.SetFloat(k_Blend, math.length(velocityRef.ValueRO.Linear));
            }
        }
    }
}

public struct ModelForEntity : IComponentData {
    public Entity modelEntity;
}

public struct RotateTowardsData : IComponentData {
    public float speed;
    public float3 direction;
}

partial struct RotateTowardsSystem : ISystem {
    public void OnUpdate(ref SystemState state) {
        var deltaTime = SystemAPI.Time.DeltaTime;
        foreach (var (model, data) in SystemAPI.Query<ModelForEntity, RotateTowardsData>()) {
            if (math.lengthsq(data.direction) < 0.01f) continue;
            
            var modelLT = SystemAPI.GetComponent<LocalTransform>(model.modelEntity);
            modelLT.Rotation = math.mul(modelLT.Rotation,
                quaternion.RotateY(Vector2.SignedAngle(data.direction.xz, modelLT.Forward().xz) * data.speed * deltaTime));
            SystemAPI.SetComponent(model.modelEntity, modelLT);
        }
    }
}