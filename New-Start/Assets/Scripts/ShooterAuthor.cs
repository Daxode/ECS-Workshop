using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShooterAuthor : MonoBehaviour
{
    [SerializeField] InputAction shootInput;
    [SerializeField] InputAction shootAltInput;

    [SerializeField] ProjectileAuthor projectile;
    [SerializeField] Transform shotPos;
#pragma warning disable CS0414
    [SerializeField] float shotForce = 10f;
#pragma warning restore CS0414
}