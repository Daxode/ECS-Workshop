using System;
using System.Diagnostics.CodeAnalysis;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

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
                DocumentGo = DependsOn(authoring.DocumentToDraw.gameObject),
                BuildingButtons = authoring.BuildingButtons,
                BuildingButtonsContainer = authoring.BuildingButtonsContainer
            });
        }
    }
}

class CanvasDocumentReference : IComponentData, IEquatable<CanvasDocumentReference>
{
    public GameObject DocumentGo;
    public Sprite[] BuildingButtons;
    public VisualTreeAsset BuildingButtonsContainer;

    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (DocumentGo != null ? DocumentGo.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (BuildingButtons != null ? BuildingButtons.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (BuildingButtonsContainer != null ? BuildingButtonsContainer.GetHashCode() : 0);
            return hashCode;
        }
    }

    public bool Equals(CanvasDocumentReference other) 
        => GetHashCode() == other?.GetHashCode() 
           && Equals(DocumentGo, other.DocumentGo) 
           && Equals(BuildingButtons, other.BuildingButtons) 
           && Equals(BuildingButtonsContainer, other.BuildingButtonsContainer);

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((CanvasDocumentReference)obj);
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Default)]
partial struct CanvasSetupSystem : ISystem
{
#if UNITY_EDITOR
    class Singleton : IComponentData
    {
        public CanvasDocumentReference LastDocument;
    }
#endif
    
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CanvasDocumentReference>();
#if UNITY_EDITOR
        if (state.WorldUnmanaged.Flags == WorldFlags.Editor) 
            state.EntityManager.AddComponentObject(state.SystemHandle, new Singleton());
#endif
    }

    public void OnUpdate(ref SystemState state)
    {
        // If canvas needs update
        var documentReference = SystemAPI.ManagedAPI.GetSingleton<CanvasDocumentReference>();
        if (state.WorldUnmanaged.Flags == WorldFlags.Editor) {
            var singleton = SystemAPI.ManagedAPI.GetComponent<Singleton>(state.SystemHandle);
            if (documentReference.Equals(singleton.LastDocument))
                return;
            singleton.LastDocument = documentReference;
        }
        else
        {
            state.Enabled = false;
        }

        // If canvas is in editor, update the canvas in editor
        var document = state.WorldUnmanaged.Flags switch 
        {
            WorldFlags.Editor => Object.FindObjectOfType<UIDocument>(),
            _ => Object.Instantiate(documentReference.DocumentGo).GetComponent<UIDocument>()
        };
        
        // Get clean BuildingButtons container
        var buildingButtons = document.rootVisualElement.Q<VisualElement>("BuildingButtons");
        buildingButtons.Clear();
        
        // Add buttons to BuildingButtons container
        var i = 0;
        foreach (var buildingButton in documentReference.BuildingButtons)
        {
            // Instantiate building button and set icon
            var buildingButtonInstance = documentReference.BuildingButtonsContainer.Instantiate();
            buildingButtonInstance.Q<VisualElement>("Icon").style.backgroundImage = new StyleBackground(buildingButton);
            
            // Sets cursor to draw to the one corresponding with the button pressed
            var cursorToDraw = i switch
            {
                3 => CursorSelection.CursorToDraw.DestroyDefault,
                4 => CursorSelection.CursorToDraw.SelectDefault,
                5 => CursorSelection.CursorToDraw.DrawDefault,
                _ => CursorSelection.CursorToDraw.LadderOutline + i
            };

            // On button click, set the cursor to draw to the one corresponding with the button pressed
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
