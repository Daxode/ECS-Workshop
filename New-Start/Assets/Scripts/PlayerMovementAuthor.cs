using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(StillRotationModelAuthor))]
class PlayerMovementAuthor : MonoBehaviour
{
    [SerializeField] InputAction directionalInput;
    [SerializeField] float speed;
}