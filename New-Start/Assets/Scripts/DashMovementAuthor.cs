using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

public class DashMovementAuthor : MonoBehaviour
{
    [SerializeField] InputAction dashInput;
    [SerializeField] float dashSpeed;
    [SerializeField] float dashInvincibilityDuration;
    [SerializeField] float dashCooldown;

    [Header("UI - Temp stored here")]
    [SerializeField] Sprite[] staminaSprites;

    class Baker : Baker<DashMovementAuthor>
    {
        public override void Bake(DashMovementAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            foreach (var staminaSprite in authoring.staminaSprites) 
                DependsOn(staminaSprite);

            AddComponentObject(entity, new DashMovementManaged
            {
                dashInput = authoring.dashInput,
                dashSpeed = authoring.dashSpeed,
                dashInvincibilityDuration = authoring.dashInvincibilityDuration,
                dashCooldown = authoring.dashCooldown,
                staminaSprites = authoring.staminaSprites ?? Array.Empty<Sprite>(),
                lastStaminaSpriteIndex = -1
            });

            AddComponent<DashMovementData>(entity);
        }
    }
}