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
                DocumentGo = DependsOn(authoring.DocumentToDraw.gameObject),
                BuildingButtons = authoring.BuildingButtons,
                BuildingButtonsContainer = authoring.BuildingButtonsContainer
            });
        }
    }
}
