using UnityEngine;

public partial class TestEvents {

    [CreateAssetMenu(fileName = "MyEvents", menuName = "MyEvents")]
    internal class MyEvents : ScriptableObject { 
        [SerializeReference]
        [SerializeField] public SerializedDelegate testAction;
    }
}