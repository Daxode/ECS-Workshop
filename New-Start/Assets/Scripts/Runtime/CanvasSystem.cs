using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

partial struct CanvasSystem : ISystem, ISystemStartStop
{
    public class ManagedData : IComponentData
    {
        public VisualElement healthBar;
        public VisualElement staminaBar;
        public UIDocument uiDocument;
    }
    
    public void OnCreate(ref SystemState state)
        => state.RequireForUpdate<HealthData>();

    public void OnStartRunning(ref SystemState state)
    {
        // create UI
        var data = new ManagedData
        {
            uiDocument = Object.FindObjectOfType<UIDocument>()
        };
        data.staminaBar = data.uiDocument.rootVisualElement.Q<VisualElement>("stamina");
        data.healthBar = data.uiDocument.rootVisualElement.Q<VisualElement>("health");
        state.EntityManager.AddComponentObject(state.SystemHandle, data);
    }

    public void OnStopRunning(ref SystemState state) {}
}
