using System;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(StillRotationModelAuthor))]
public class FollowPlayerAuthor : MonoBehaviour
{
    [SerializeField] float speed;
    [SerializeField] float2 slowDownRange = new(4, 6);
#pragma warning disable CS0414
    [SerializeField] float shootInterval = 0.2f;
#pragma warning restore CS0414
}