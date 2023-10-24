using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(StillRotationModelAuthor))]
class PlayerMovementAuthor : MonoBehaviour
{
    [SerializeField] InputAction directionalInput;
    [SerializeField] float speed;

    class Baker : Baker<PlayerMovementAuthor>
    {
        public override void Bake(PlayerMovementAuthor authoring)
        {
            var playerEntity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(playerEntity,  new PlayerMovement
            {
                speed = authoring.speed
            });
        }
    }
}