using System;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class GrassFieldAuthor : MonoBehaviour
{
    [SerializeField] GameObject grassPrefab;
#pragma warning disable CS0414
    [SerializeField] int grassCount = 100;
#pragma warning restore CS0414
    [SerializeField] uint seed;

    [Range(-0.99f, 0.99f)]
    [SerializeField] float threshold;

    [Range(0.01f, 10f)]
    [SerializeField] float noiseScale;
}