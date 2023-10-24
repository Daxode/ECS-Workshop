using System;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class LockRigidBodyAuthor : MonoBehaviour
{
    public bool3 lockPosition = new(false, true, false);
    public bool3 lockRotation = true;
}