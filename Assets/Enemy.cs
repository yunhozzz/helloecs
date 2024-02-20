using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int attack = 10;
    public float speed = 10f;
    public float health = 100;
}

public class EnemyBuilder : Baker<Enemy>
{
    public override void Bake(Enemy authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent<EnemyTag>(entity);
        AddComponent<MovingTag>(entity);
        AddComponent(entity, new EnemyAttack { Value = authoring.attack });
        AddComponent(entity, new EnemySpeed { Value = authoring.speed });
        AddComponent(entity, new EnemyKnockBack() { dist = 0 });
        AddComponent(entity, new Health { Value = authoring.health, Max = authoring.health });
        AddBuffer<DamageBufferElement>(entity);
    }
}

public struct EnemySpeed : IComponentData
{
    public float Value;
}

public struct EnemyAttack : IComponentData
{
    public int Value;
}

public struct EnemyKnockBack : IComponentData
{
    public float dist;
}

public struct EnemyTag : IComponentData
{
}


public readonly partial struct EnemyMoveAspect : IAspect
{
    public readonly Entity Entity;

    private readonly RefRW<LocalTransform> _transform;
    private readonly RefRW<EnemyKnockBack> _knockBack;
    private readonly RefRO<EnemySpeed> _speed;
    private readonly RefRO<EnemyAttack> _attack;
    private readonly RefRO<MovingTag> _moveTag;

    public int GetDamage()
    {
        return _attack.ValueRO.Value;
    }

    public void Move(float deltaTime, float3 moveDir)
    {
        if (_moveTag.ValueRO.Value == false)
            return;
        _transform.ValueRW.Position += (moveDir * _speed.ValueRO.Value * deltaTime);
    }

    public bool IsPassedGoalLine(float goalLineZ)
    {
        return _transform.ValueRW.Position.z < goalLineZ;
    }

    public void KnockBack(float deltaTime, float3 moveDir)
    {
        if (_moveTag.ValueRO.Value == false)
            return;
        
        _transform.ValueRW.Position += -moveDir * _knockBack.ValueRW.dist;
        _knockBack.ValueRW.dist = 0;
    }
}


[BurstCompile]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct EnemyMoveSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<HeroTag>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<EnemyTag>();
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        var heroEntity = SystemAPI.GetSingletonEntity<HeroTag>();
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();

        new EnemyMoveJob
        {
            DeltaTime = deltaTime,
            MoveDir = new Vector3(0, 0, -1),
            GoalLineZ = 0,
            heroEntity = heroEntity,
            ECBDestroy = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
            ECBDamage = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
        }.ScheduleParallel();
    }
}

[BurstCompile]
public partial struct EnemyMoveJob : IJobEntity
{
    public float DeltaTime;
    public float3 MoveDir;
    public float GoalLineZ;
    public Entity heroEntity;
    public EntityCommandBuffer.ParallelWriter ECBDestroy;
    public EntityCommandBuffer.ParallelWriter ECBDamage;
        
    [BurstCompile]
    private void Execute(EnemyMoveAspect enemy, [EntityIndexInQuery] int sortKey)
    {
        enemy.KnockBack(DeltaTime, MoveDir);
        enemy.Move(DeltaTime, MoveDir);
        
        if (enemy.IsPassedGoalLine(GoalLineZ))
        {
            ECBDestroy.DestroyEntity(sortKey, enemy.Entity);
            
            var damage = new DamageBufferElement
            {
                AttackPower = enemy.GetDamage(),
                KnockBack = 0,
            };
            ECBDamage.AppendToBuffer(sortKey, heroEntity, damage);
        }
            
    }
}

public struct DamageBufferElement : IBufferElementData
{
    public int AttackPower;
    public float KnockBack;
}
