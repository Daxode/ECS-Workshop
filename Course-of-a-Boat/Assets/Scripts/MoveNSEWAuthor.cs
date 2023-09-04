using UnityEngine;

public class MoveNSEWAuthor : MonoBehaviour
{
    [Header("Movement")]
    public float accelerationToSetInDirection = 1.0f;
    public float maxForce = 10.0f;
    public float drag = 0.1f;
    
    [Header("Key Bindings")]
    public KeyCode north = KeyCode.W;
    public KeyCode south = KeyCode.S;
    public KeyCode east = KeyCode.D;
    public KeyCode west = KeyCode.A;
}