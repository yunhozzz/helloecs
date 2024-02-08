using DefaultNamespace;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class Bullet : MonoBehaviour
{
	public float Speed = 10f;
	public float Damage = 10f;
	public float ExplosionRadius = 0f;
	public GameObject HitPrefab;
}

public class BulletBaker : Baker<Bullet>
{
	public override void Bake(Bullet authoring)
	{
		var entity = GetEntity(TransformUsageFlags.Dynamic);
		AddComponent(entity, new MovingTag());
		AddComponent(entity, new BulletComponent()
		{
			Speed = authoring.Speed,
			Damage = authoring.Damage,
			ExplosionRadius = authoring.ExplosionRadius,
		});
		
	}
}

public struct BulletComponent : IComponentData
{
	public float Speed;
	public float Damage;
	public float ExplosionRadius;
}

public readonly partial struct BulletAspect : IAspect
{
	public readonly Entity Entity;
	
	private readonly RefRO<MovingTag> _moveTag;
	private readonly RefRO<BulletComponent> _bullet;
	private readonly RefRW<LocalTransform> _transform;

	public void Move(float deltaTime, float3 moveDir, out float3 startPos, out float3 endPos)
	{
		startPos = endPos = _transform.ValueRO.Position;
		if (_moveTag.ValueRO.Value == false)
			return;
		
		endPos = _transform.ValueRW.Position += (moveDir * _bullet.ValueRO.Speed * deltaTime);
	}

	public int GetDamage()
	{
		return (int)_bullet.ValueRO.Damage;
	}

	public bool HasExplosion()
	{
		return _bullet.ValueRO.ExplosionRadius > 0;
	}

	public bool IsMoving()
	{
		return _moveTag.ValueRO.Value;
	}

}


[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))] // Make sure that the running order of systems is correct
public partial struct BulletSystem : ISystem
{
	public void OnCreate(ref SystemState state)
	{
		state.RequireForUpdate<PhysicsWorldSingleton>();
		state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
		state.RequireForUpdate<HeroTag>();
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		var deltaTime = SystemAPI.Time.DeltaTime;
		var boardEntity = SystemAPI.GetSingletonEntity<BoardData>();
		var heroEntity = SystemAPI.GetSingletonEntity<HeroTag>();
		var board = SystemAPI.GetComponentRO<BoardData>(boardEntity);
		var physics = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
		var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();

	
		state.Dependency = new BulletMoveJob
		{
			DeltaTime = deltaTime,
			MoveDir = new Vector3(0, 0, 1),
			heroEntity = heroEntity,
			board = board.ValueRO,
			physics = physics,
			ECBDestroy = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
			ECBDamage = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
		}.Schedule(state.Dependency);
		
		state.CompleteDependency();
	}
}

[BurstCompile]
public partial struct BulletMoveJob : IJobEntity
{
	public float DeltaTime;
	public float3 MoveDir;
	public Entity heroEntity;
	public PhysicsWorldSingleton physics;
	public EntityCommandBuffer.ParallelWriter ECBDestroy;
	public EntityCommandBuffer.ParallelWriter ECBDamage;
	public BoardData board;

	[BurstCompile]
	private void Execute(BulletAspect bullet, [EntityIndexInQuery] int sortKey)
	{
		if (!bullet.IsMoving())
			return;

		bullet.Move(DeltaTime, MoveDir, out var startPos, out var endPos);
		if (endPos.z >= board.FarLine)
		{
			ECBDestroy.DestroyEntity(sortKey, bullet.Entity);
			return;
		}

		var cast = new RaycastInput()
		{
			Start = startPos,
			End = endPos,
			Filter = CollisionFilter.Default,
		};

		if (physics.CastRay(cast, out var hit))
		{
			if (bullet.HasExplosion())
			{

			}
			else
			{
				var damage = new DamageBufferElement { Value = bullet.GetDamage() };
				ECBDamage.AppendToBuffer(sortKey, hit.Entity, damage);
			}

			var hitEff = ECBDestroy.CreateEntity(sortKey);
			ECBDestroy.SetComponent(sortKey, hitEff, new LocalTransform()
			{
				Position = hit.Position,
				Rotation = quaternion.identity,
				Scale = 1,
			});
			ECBDestroy.SetComponent(sortKey, hitEff, new BulletHitEffect()
			{
			});
			ECBDestroy.DestroyEntity(sortKey, bullet.Entity);
		}

	}
	
	public struct BulletHitEffect : IComponentData
	{
		
	}

}