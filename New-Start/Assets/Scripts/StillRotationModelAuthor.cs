using System;
using UnityEngine;

[RequireComponent(typeof(LockRigidBodyAuthor))]
class StillRotationModelAuthor : MonoBehaviour
{
    [SerializeField] Transform model;
    [SerializeField] float rotateSpeed;
}
