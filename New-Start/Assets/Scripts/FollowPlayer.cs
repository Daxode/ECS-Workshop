using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using Material = Unity.Physics.Material;

public class FollowPlayer : MonoBehaviour
{
    private class Baker : Baker<FollowPlayer>
    {
        public override void Bake(FollowPlayer authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            
            // lock to xz plane
            var jointEntity = CreateAdditionalEntity(TransformUsageFlags.None);
            AddComponent(jointEntity, PhysicsJoint.CreateLimitedDOF(RigidTransform.identity,
                false, true));
            AddComponent(jointEntity, new PhysicsConstrainedBodyPair(entity, Entity.Null, false));
            AddComponent<PhysicsWorldIndex>(jointEntity);
            
        }
    }
}
