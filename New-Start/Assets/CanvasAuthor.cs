using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

public class CanvasAuthor : MonoBehaviour
{
    [SerializeField] UIDocument DocumentToDraw;
    [SerializeField] Sprite[] BuildingButtons;
    [SerializeField] VisualTreeAsset BuildingButtonsContainer;

    private class CanvasAuthorBaker : Baker<CanvasAuthor>
    {
        public override void Bake(CanvasAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponentObject(entity, new CanvasDocumentReference
            {
                Document = DependsOn(authoring.DocumentToDraw),
                BuildingButtons = authoring.BuildingButtons,
                BuildingButtonsContainer = authoring.BuildingButtonsContainer
            });
        }
    }
}

class CanvasDocumentReference : IComponentData
{
    public UIDocument Document;
    public Sprite[] BuildingButtons;
    public VisualTreeAsset BuildingButtonsContainer;
}

partial struct CanvasSetupSystem : ISystem, ISystemStartStop
{
    class Singleton : IComponentData
    {
        public List<GameObject> cleanupObjects;
    }
    
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CanvasDocumentReference>();
        state.EntityManager.AddComponentObject(state.SystemHandle, new Singleton{ cleanupObjects = new List<GameObject>() });
    }
    
    public void OnStartRunning(ref SystemState state)
    {
        var cleanupObjects = SystemAPI.ManagedAPI.GetComponent<Singleton>(state.SystemHandle).cleanupObjects;
        foreach (var documentReference in SystemAPI.Query<CanvasDocumentReference>())
        {
            var document = Object.Instantiate(documentReference.Document);
            cleanupObjects.Add(document.gameObject);
            var buildingButtons = document.rootVisualElement.Q<VisualElement>("BuildingButtons");

            var i = 0;
            foreach (var buildingButton in documentReference.BuildingButtons)
            {
                var buildingButtonInstance = documentReference.BuildingButtonsContainer.Instantiate();
                buildingButtonInstance.Q<VisualElement>("Icon").style.backgroundImage = new StyleBackground(buildingButton);
                
                // Sets cursor to draw to the one corresponding with the button pressed
                var cursorToDraw = CursorSelection.CursorToDraw.LadderOutline + i;
                if (i == 4)
                    cursorToDraw = CursorSelection.CursorToDraw.Default;
                if (i == 5)
                    cursorToDraw = CursorSelection.CursorToDraw.DefaultDraw;
                
                var cursorSelection = SystemAPI.QueryBuilder().WithAllRW<CursorSelection>().Build();
                buildingButtonInstance.Q<Button>().clickable.clicked += () =>
                {
                    cursorSelection.GetSingletonRW<CursorSelection>().ValueRW.cursorToDraw = cursorToDraw;
                };
                buildingButtons.Add(buildingButtonInstance);
                i++;
            }
        }
    }

    public void OnStopRunning(ref SystemState state)
    {
        var cleanupObjects = SystemAPI.ManagedAPI.GetComponent<Singleton>(state.SystemHandle).cleanupObjects;
        foreach (var go in cleanupObjects)
            Object.DestroyImmediate(go);
        cleanupObjects.Clear();
    }
}
