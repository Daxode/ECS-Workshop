using System;
using Unity.Entities;
using UnityEngine;

public class CursorHeadAuthor : MonoBehaviour
{
    class CursorHeadAuthorBaker : Baker<CursorHeadAuthor>
    {
        public override void Bake(CursorHeadAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(entity, new CursorTagHead());
        }
    }
}