using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public struct HealthData : IComponentData {
    public int health;
    public int maxHealth;
    public float hitInvincibilityTimer;
}

public class HealthAuthor : MonoBehaviour {
    public class HealthManagedData : IComponentData {
        public Sprite[] healthSprites;
        public Entity damageParticles;
    }
    
    [SerializeField] int maxHealth;
    [SerializeField] Sprite[] healthSprites;
    [SerializeField] ParticleSystem damageParticles;
    
    class HealthAuthorBaker : Baker<HealthAuthor> {
        
        public override void Bake(HealthAuthor authoring) {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            foreach (var healthSprite in authoring.healthSprites) {
                DependsOn(healthSprite);
            }
            AddComponentObject(entity, new HealthManagedData {
                healthSprites = authoring.healthSprites ?? Array.Empty<Sprite>(),
                damageParticles = GetEntity(authoring.damageParticles, TransformUsageFlags.Renderable),
            });
            AddComponent(entity, new HealthData {
                health = authoring.maxHealth,
                maxHealth = authoring.maxHealth,
            });
        }
    }
}

public partial struct HealthSystem : ISystem, ISystemStartStop {
    public void OnCreate(ref SystemState state) {
        state.RequireForUpdate<HealthAuthor.HealthManagedData>();
        state.RequireForUpdate<DashMovementSystem.UISingleton>();
    }

    public void OnStartRunning(ref SystemState state) {
        var uiSingleton = SystemAPI.ManagedAPI.GetSingleton<DashMovementSystem.UISingleton>();
        uiSingleton.healthBar = uiSingleton.uiDocument.rootVisualElement.Q("health");
        uiSingleton.lastHealthSpriteIndex = -1;
    }

    public void OnStopRunning(ref SystemState state) {}

    public void OnUpdate(ref SystemState state) {
        // ReSharper disable once Unity.Entities.SingletonMustBeRequested
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        
        var uiSingleton = SystemAPI.ManagedAPI.GetSingleton<DashMovementSystem.UISingleton>();
        foreach (var (healthManagedData, dataRef, e) in SystemAPI
                     .Query<HealthAuthor.HealthManagedData, RefRW<HealthData>>().WithAll<PlayerMovementAuthor.PlayerInputManaged>().WithEntityAccess()) {
            // update invincibility timer
            if (dataRef.ValueRW.hitInvincibilityTimer > 0) {
                dataRef.ValueRW.hitInvincibilityTimer -= SystemAPI.Time.DeltaTime;
            }
            
            // check if dead
            if (dataRef.ValueRO.health <= 0) {
                ecb.DestroyEntity(e);
            }
            
            // update UI
            if (dataRef.ValueRO.health <= 0) continue;
            var percentage = math.saturate(dataRef.ValueRO.health / (float) dataRef.ValueRO.maxHealth);
            var healthSpriteIndex = (int)math.floor(percentage * (healthManagedData.healthSprites.Length-1));
            if (healthSpriteIndex != uiSingleton.lastHealthSpriteIndex) {
                uiSingleton.healthBar.style.backgroundImage = new StyleBackground(healthManagedData.healthSprites[healthSpriteIndex]);
                uiSingleton.lastHealthSpriteIndex = healthSpriteIndex;
                
                // skip if health is full
                if (healthSpriteIndex == healthManagedData.healthSprites.Length-1) continue;
                
                // play damage particles
                if (healthManagedData.damageParticles != Entity.Null && SystemAPI.ManagedAPI.HasComponent<ParticleSystem>(healthManagedData.damageParticles)) {
                    var damageParticles = SystemAPI.ManagedAPI.GetComponent<ParticleSystem>(healthManagedData.damageParticles);
                    damageParticles.Play();
                }
            }
            
        }
    }
}
