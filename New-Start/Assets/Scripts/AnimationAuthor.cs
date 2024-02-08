using Unity.Entities;
using UnityEngine;

class AnimationAuthor : MonoBehaviour
{
    [SerializeField] Animator animator;

    class AnimationBaker : Baker<AnimationAuthor>
    {
        public override void Bake(AnimationAuthor authoring)
        {
            if (authoring.animator == null)
                return;

            var entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponentObject(entity, new AnimationManaged
            {
                animator = authoring.animator
            });

            if (IsBakingForEditor())
                AddComponent(entity, new DrawInEditor
                {
                    entity = GetEntity(authoring.animator,
                        TransformUsageFlags.Renderable | TransformUsageFlags.WorldSpace)
                });
        }
    }
}

