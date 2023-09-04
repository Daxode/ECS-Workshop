using UnityEngine;

public class MoveUpAndDownAuthor : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 1.0f;
    public float amplitude = 0.2f;
    public float offset = 0.0f;
    
    [Header("Extra Settings")]
    public bool addGameObjectYToOffset = true;
}