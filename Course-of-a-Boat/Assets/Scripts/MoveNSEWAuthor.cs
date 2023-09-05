using Runtime;
using Unity.Entities;
using UnityEngine;

public class MoveNSEWAuthor : MonoBehaviour
{
    [Header("Movement")]
    public float accelerationToSetInDirection = 1.0f;
    public float maxForce = 10.0f;
    public float drag = 0.1f;
    
    [Header("Key Bindings")]
    public KeyCode north = KeyCode.W;
    public KeyCode south = KeyCode.S;
    public KeyCode east = KeyCode.D;
    public KeyCode west = KeyCode.A;

    class MoveNSEWAuthorBaker : Baker<MoveNSEWAuthor>
    {
        public override void Bake(MoveNSEWAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new PushInDirection
            {
                maxForce = authoring.maxForce,
                drag = authoring.drag,
            });
        }
    }
}