using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(StillRotationModelAuthor))]
class PlayerTurnAroundAuthor : MonoBehaviour
{
    [SerializeField] InputAction directionalInput;
}