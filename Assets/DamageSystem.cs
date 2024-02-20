using DefaultNamespace;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public readonly partial struct DamageAspect : IAspect
{
	public readonly Entity Entity;
	private readonly RefRW<LocalTransform> _transform;
	private readonly RefRW<Health> _health;
	private readonly DynamicBuffer<DamageBufferElement> _damageBuffer;
    
    
	public bool ApplyDamages(EntityCommandBuffer ecb, in float3 moveDir)
	{
		var damaged = false;
		foreach (var damage in _damageBuffer)
		{
			_health.ValueRW.Value -= damage.AttackPower;
			_transform.ValueRW.Position += moveDir * -damage.KnockBack;
            
			if (_health.ValueRW.Value <= 0 && _health.ValueRW.IsHero == false)
			{
				ecb.DestroyEntity(Entity);
			}

			damaged = true;
		}
		_damageBuffer.Clear();
		return damaged;
	}

	public float3 GetPosition()
	{
		return _transform.ValueRO.Position;
	}
}


[UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
[UpdateAfter(typeof(EndSimulationEntityCommandBufferSystem))]
public partial class DamageSystem : SystemBase
{
	UnityEngine.ParticleSystem particleSystem;
	Transform particleSystemTransform;
	
	

	public void Init(UnityEngine.ParticleSystem particleSystem)
	{
		this.particleSystem = particleSystem;
		particleSystemTransform = particleSystem.transform;
		Enabled = true; // Everything is ready, can begin running the system
	}
	
	protected override void OnUpdate()
	{
		var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
		var boardQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<BoardData>());
		if (!boardQuery.TryGetSingleton<BoardData>(out var board))
			return;
		
		if (!particleSystem)
			return;
        
		
		var ecbSingleton = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
		var ecb = ecbSingleton.CreateCommandBuffer();

		foreach (var damageAspect in SystemAPI.Query<DamageAspect>())
		{
			var damaged = damageAspect.ApplyDamages(ecb, board.EnemyMoveDir);
			if (damaged)
			{
				particleSystemTransform.position = damageAspect.GetPosition();
				particleSystem.Emit(10);
			}
		}
	}
}