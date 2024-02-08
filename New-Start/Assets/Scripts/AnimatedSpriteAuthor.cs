using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class AnimatedSpriteAuthor : MonoBehaviour
{
    [SerializeField] Texture2D spriteTextureSheet;
    [SerializeField] Sprite[] spriteFrames;
    
    class Baker : Baker<AnimatedSpriteAuthor>
    {
        public override void Bake(AnimatedSpriteAuthor authoring)
        {
            var texelSize = DependsOn(authoring.spriteTextureSheet).texelSize;
            
            var entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(entity, new MaterialOverrideOffsetXYScaleZW { Value = new float4(
                authoring.spriteFrames.Length > 0 // offset
                    ? authoring.spriteFrames[0].rect.position * texelSize 
                    : float2.zero, 
                 texelSize * 32 // scale
            )});

            var buffer = AddBuffer<SpriteFrameElement>(entity);
            foreach (var spriteFrame in authoring.spriteFrames)
                buffer.Add(new SpriteFrameElement
                {
                    offset = spriteFrame.rect.position * texelSize
                });
        }
    }
}