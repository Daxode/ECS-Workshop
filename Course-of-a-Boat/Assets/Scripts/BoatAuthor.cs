using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

[Serializable]
public struct FollowMarkerData : IComponentData
{
    public float speed;
    public float turnSpeed;
}

public class BoatAuthor : MonoBehaviour
{
    public GameObject PreviewModel;
    public FollowMarkerData FollowMarkerData;

    class BoatAuthorBaker : Baker<BoatAuthor>
    {
        public override void Bake(BoatAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, authoring.FollowMarkerData);

            var joint = CreateAdditionalEntity(TransformUsageFlags.None);
            AddComponent(joint, PhysicsJoint.CreateLimitedDOF(default, new bool3(false, true, false), new bool3(true,false,true)));
            AddComponent(joint, new PhysicsConstrainedBodyPair(entity, Entity.Null, false));
            AddSharedComponent(joint, default(PhysicsWorldIndex));
        }
    }
}