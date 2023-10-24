using System;
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
}