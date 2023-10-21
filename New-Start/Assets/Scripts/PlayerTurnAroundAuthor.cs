using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(StillRotationModelAuthor))]
class PlayerTurnAroundAuthor : MonoBehaviour
{
    [SerializeField] InputAction directionalInput;

    class Baker : Baker<PlayerTurnAroundAuthor>
    {
        public override void Bake(PlayerTurnAroundAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponentObject(entity, new PlayerTurnAroundManaged
            {
                directionalInput = authoring.directionalInput,
                followMouseInstead = true
            });
        }
    }
}