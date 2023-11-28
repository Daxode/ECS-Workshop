using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

partial struct CanvasSystem : ISystem, ISystemStartStop
{
    public class ManagedData : IComponentData
    {
        public UIDocument uiDocument;
    }
    
    public void OnCreate(ref SystemState state)
        => state.RequireForUpdate<HealthData>();

    public void OnStartRunning(ref SystemState state)
    {
        var doc = Object.FindObjectOfType<UIDocument>();
        
        // create UI
        state.EntityManager.AddComponentObject(state.SystemHandle, new ManagedData 
        { 
            uiDocument = doc 
        });
    }

    public void OnStopRunning(ref SystemState state) {}
}
