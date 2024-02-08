using Unity.Entities;
using UnityEngine;

public class CursorAuthor : MonoBehaviour
{
    class CursorAuthorBaker : Baker<CursorAuthor>
    {
        public override void Bake(CursorAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(entity, new CursorSelection{cursorToDraw = CursorSelection.CursorToDraw.SelectDefault});
        }
    }
}