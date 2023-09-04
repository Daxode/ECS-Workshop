using System;
using UnityEngine;

[Serializable]
public struct FollowMarkerData
{
    public float speed;
    public float turnSpeed;
}

public class BoatAuthor : MonoBehaviour
{
    public GameObject PreviewModel;
    public FollowMarkerData FollowMarkerData;
}