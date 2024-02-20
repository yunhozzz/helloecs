using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using Unity.Entities;
using Unity.Mathematics;
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
        AddComponent(entity, new Health
        {
            IsHero = true,
            Value = authoring.Health,
            Max = authoring.Health
        });
        AddComponent(entity, new HeroSpeed { Value = authoring.Speed });
        AddComponent(entity, new HeroInput { MoveX = 0 });
        AddComponent(entity, new HeroAbility { Speed = 10 });
        AddComponent(entity, new HeroShoot
        {
            Interval = authoring.ShootInterval,
            Bullet = GetEntity(authoring.BulletPrefab, TransformUsageFlags.Dynamic | TransformUsageFlags.Renderable),
        });
        AddBuffer<DamageBufferElement>(entity);
    }
}


public struct Health : IComponentData
{
    public bool IsHero;
    public float Value;
    public float Max;
}

public struct HeroTag : IComponentData {}
public struct HeroSpeed : IComponentData { public float Value; }
public struct HeroInput : IComponentData { public float MoveX; }

public struct HeroAbility : IComponentData
{
    public float Speed;
}

public struct HeroShoot : IComponentData
{
    public float Elpased;
    public float Interval;
    public Entity Bullet;
}

public readonly partial struct HeroAspect : IAspect
{
    public readonly Entity Entity;

    private readonly RefRO<HeroInput> _input;
    private readonly RefRO<HeroAbility> _ability;
    private readonly RefRW<LocalTransform> _transform;
    private readonly RefRW<HeroShoot> _shoot;
    
    
    public void SpawnBullet(float deltaTime, SystemState state)
    {
        _shoot.ValueRW.Elpased += deltaTime;
        if (_shoot.ValueRW.Elpased < _shoot.ValueRW.Interval)
            return;

        _shoot.ValueRW.Elpased = 0;
        var bullet = state.EntityManager.Instantiate(_shoot.ValueRO.Bullet);
        var bulletTr = state.EntityManager.GetComponentData<LocalTransform>(bullet); 
        state.EntityManager.SetComponentData(bullet, new LocalTransform()
        {
            Position = _transform.ValueRW.Position,
            Rotation = _transform.ValueRW.Rotation,
            Scale = bulletTr.Scale,
        });
        state.EntityManager.SetComponentData(bullet, new MovingTag()
        {
            Value = true,
        });
        state.EntityManager.SetComponentData(bullet, new BulletComponent()
        {
            Speed = 10f,
            Damage = 20f,
            ExplosionRadius = 0f,
        });
    }

    public void Move(float deltaTime, BoardData board)
    {
        var pos = _transform.ValueRW.Position;
        pos.x += _input.ValueRO.MoveX * deltaTime * _ability.ValueRO.Speed;
        pos.x = math.clamp(pos.x, board.Left, board.Right);
            
        _transform.ValueRW.Position = pos;
    }
}

[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct HeroMoveSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<BoardData>(out var board))
            return;
            
        var deltaTime = SystemAPI.Time.DeltaTime;
        foreach (var hero in SystemAPI.Query<HeroAspect>())
        {
            hero.SpawnBullet(deltaTime, state);
            hero.Move(deltaTime, board);
        }
    }
}