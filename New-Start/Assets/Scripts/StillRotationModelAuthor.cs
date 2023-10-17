using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class StillRotationModelAuthor : MonoBehaviour {
    [SerializeField] Transform model;
    [SerializeField] float rotateSpeed;
    class Baker : Baker<StillRotationModelAuthor> {
        public override void Bake(StillRotationModelAuthor authoring) {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            
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