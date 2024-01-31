using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class Hero : MonoBehaviour
{
    public float Health;
    public float Speed;
    public float Attack;
    public float ShootInterval;
    public GameObject BulletPrefab;
}

public class HeroBaker : Baker<Hero>
{
    public override void Bake(Hero authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent<HeroTag>(entity);
        AddComponent(entity, new HeroHealth { Value = authoring.Health, Max = authoring.Health });
        AddComponent(entity, new HeroSpeed { Value = authoring.Speed });
        AddComponent(entity, new HeroShoot
        {
            Interval = authoring.ShootInterval,
            Bullet = GetEntity(authoring.BulletPrefab, TransformUsageFlags.Dynamic | TransformUsageFlags.Renderable),
        });
        AddBuffer<DamageBufferElement>(entity);
    }
}

public struct HeroTag : IComponentData {}
public struct HeroHealth : IComponentData { public float Value; public float Max; }
public struct HeroSpeed : IComponentData { public float Value; }

public struct HeroShoot : IComponentData
{
    public float Elpased;
    public float Interval;
    public Entity Bullet;
}


public readonly partial struct HeroAspect : IAspect
{
    public readonly Entity Entity;

    private readonly RefRW<LocalTransform> _transform;
    private readonly RefRW<HeroHealth> _health;
    private readonly RefRW<HeroShoot> _shoot;
    private readonly DynamicBuffer<DamageBufferElement> _damageBuffer;
    
    
    public void ApplyDamages()
    {
        foreach (var brainDamageBufferElement in _damageBuffer)
        {
            _health.ValueRW.Value -= brainDamageBufferElement.Value;
        }
        _damageBuffer.Clear();
    }
    
    public void SpawnBullet(float deltaTime, SystemState state)
    {
        _shoot.ValueRW.Elpased += deltaTime;
        if (_shoot.ValueRW.Elpased < _shoot.ValueRW.Interval)
            return;

        _shoot.ValueRW.Elpased = 0;
        var bullet = state.EntityManager.Instantiate(_shoot.ValueRO.Bullet);
        state.EntityManager.SetComponentData(bullet, new LocalTransform()
        {
            Position = _transform.ValueRW.Position,
            Rotation = _transform.ValueRW.Rotation,
            Scale = _transform.ValueRW.Scale,
        });
        state.EntityManager.SetComponentData(bullet, new MovingTag()
        {
            Value = true,
        });
        state.EntityManager.SetComponentData(bullet, new BulletComponent()
        {
            Speed = 10f,
            Damage = 10f,
            ExplosionRadius = 0f,
        });
    }
}

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
[UpdateAfter(typeof(EndSimulationEntityCommandBufferSystem))]
public partial struct HeroSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        foreach (var hero in SystemAPI.Query<HeroAspect>())
        {
            hero.ApplyDamages();
            hero.SpawnBullet(deltaTime, state);
        }
    }
}