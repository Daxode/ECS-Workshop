﻿// using System;
// using Unity.Burst;
// using Unity.Collections;
// using Unity.Collections.LowLevel.Unsafe;
// using Unity.Entities;
// using Unity.Physics;
// using Unity.Physics.Systems;
//
// struct AttackDamage : IComponentData
// {
//     public int damage;
//     public Entity owningEntity;
// }
//
// struct SetRaisedCollisionEvents : IComponentData {}
//
// partial struct SetupCollisionResponseSystem : ISystem
// {
//     [BurstCompile]
//     public void OnCreate(ref SystemState state)
//         => state.RequireForUpdate<SetRaisedCollisionEvents>();
//
//     [BurstCompile]
//     public unsafe void OnUpdate(ref SystemState state)
//     {
//         foreach (var colRef in SystemAPI.Query<PhysicsCollider>().WithAll<SetRaisedCollisionEvents>())
//         {
//             ref var collider = ref UnsafeUtility.AsRef<Collider>(colRef.ColliderPtr);
//             collider.SetCollisionResponse(CollisionResponsePolicy.CollideRaiseCollisionEvents);
//         }
//         state.EntityManager.RemoveComponent<SetRaisedCollisionEvents>(
//             SystemAPI.QueryBuilder().WithAll<SetRaisedCollisionEvents>().Build());
//     }
// }
//
// [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
// [UpdateAfter(typeof(PhysicsSystemGroup))]
// // We are updating after `PhysicsSimulationGroup` - this means that we will get the events of the current frame.
// partial struct HitPlayerEventsSystem : ISystem
// {
//     [BurstCompile]
//     public void OnUpdate(ref SystemState state)
//     {
//         // check if hit player
//         var hitPlayerJob = new HitPlayer
//         {
//             damageDataLookup = SystemAPI.GetComponentLookup<AttackDamage>(),
//             healthDataLookup = SystemAPI.GetComponentLookup<HealthData>(),
//
//             // ReSharper disable once Unity.Entities.SingletonMustBeRequested - Reason: This singleton is always available.
//             ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
//                 .CreateCommandBuffer(state.WorldUnmanaged)
//         };
//
//         // ReSharper disable once Unity.Entities.SingletonMustBeRequested - Reason: This singleton is always available.
//         var simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();
//         state.Dependency = hitPlayerJob.Schedule(simulationSingleton, state.Dependency);
//     }
// }
//
// // using collision job check if hit player
// [BurstCompile]
// struct HitPlayer : ICollisionEventsJob
// {
//     [ReadOnly] public ComponentLookup<AttackDamage> damageDataLookup;
//     public ComponentLookup<HealthData> healthDataLookup;
//     public EntityCommandBuffer ecb;
//
//     public void Execute(CollisionEvent collisionEvent)
//     {
//         var healthEntity = Entity.Null;
//         var damageEntity = Entity.Null;
//         if (damageDataLookup.HasComponent(collisionEvent.EntityA))
//             if (healthDataLookup.HasComponent(collisionEvent.EntityB))
//             {
//                 healthEntity = collisionEvent.EntityB;
//                 damageEntity = collisionEvent.EntityA;
//             }
//
//         if (healthEntity == Entity.Null && damageDataLookup.HasComponent(collisionEvent.EntityB))
//             if (healthDataLookup.HasComponent(collisionEvent.EntityA))
//             {
//                 healthEntity = collisionEvent.EntityA;
//                 damageEntity = collisionEvent.EntityB;
//             }
//
//         if (healthEntity == Entity.Null) return;
//
//         // check if damage is from owner
//         var healthData = healthDataLookup[healthEntity];
//         var damageData = damageDataLookup[damageEntity];
//         if (damageData.owningEntity == Entity.Null) return;
//         if (damageData.owningEntity == healthEntity) return;
//
//         // destroy projectile if doesn't have health
//         if (!healthDataLookup.HasComponent(damageEntity))
//             ecb.DestroyEntity(damageEntity);
//
//         // check if hit invincibility timer is active
//         if (healthData.hitInvincibilityTimer > 0) return;
//
//         // damage target
//         healthData.health -= damageData.damage;
//         healthData.hitInvincibilityTimer = 1f;
//         healthData.hitIsTriggered = true;
//         healthDataLookup[healthEntity] = healthData;
//     }
// }
