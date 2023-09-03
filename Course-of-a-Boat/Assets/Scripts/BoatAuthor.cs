using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public struct FollowMarkerData : IComponentData
{
    public float speed;
    public float turnSpeed;
}

public class BoatAuthor : MonoBehaviour
{
    public GameObject PreviewModel;
    [FormerlySerializedAs("FollowMarker")]
    public FollowMarkerData FollowMarkerData;

    public class BoatBaker : Baker<BoatAuthor>
    {
        public override void Bake(BoatAuthor author)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, author.FollowMarkerData);

            var joint = CreateAdditionalEntity(TransformUsageFlags.None);
            AddComponent(joint, PhysicsJoint.CreateLimitedDOF(default, new bool3(false, true, false), new bool3(true,false,true)));
            AddComponent(joint, new PhysicsConstrainedBodyPair(entity, Entity.Null, false));
            AddSharedComponent(joint, default(PhysicsWorldIndex));
        }
    }
}

partial struct MoveToMarkerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PushInDirection>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var marker = SystemAPI.GetSingletonEntity<PushInDirection>();
        var markerLT = SystemAPI.GetComponent<LocalTransform>(marker);

        foreach (var (velocityRef, ltRef, massRef, followRef) in SystemAPI.Query<RefRW<PhysicsVelocity>, RefRO<LocalTransform>, RefRO<PhysicsMass>, RefRO<FollowMarkerData>>())
        {
            // go forward by speed
            var speed = math.smoothstep(5*5, 6*6, math.distancesq(markerLT.Position.xz, ltRef.ValueRO.Position.xz)); // slows down at 6m to marker, stops at 5m
            velocityRef.ValueRW.Linear = ltRef.ValueRO.Forward() * (speed * SystemAPI.Time.DeltaTime * followRef.ValueRO.speed);
            
            // rotate towards marker
            var currentForward = ltRef.ValueRO.Forward().xz;
            var targetForward = math.normalize(markerLT.Position.xz - ltRef.ValueRO.Position.xz);
            var angle = Vector2.SignedAngle(targetForward, currentForward);
            angle = angle < 0.1f && angle > -0.1f ? 0f : angle; // deadzone
            velocityRef.ValueRW.SetAngularVelocityWorldSpace(in massRef.ValueRO, ltRef.ValueRO.Rotation, math.up() * (math.sign(angle) * SystemAPI.Time.DeltaTime * followRef.ValueRO.turnSpeed));
        }
    }
}