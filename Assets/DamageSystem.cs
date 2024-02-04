using Unity.Burst;
using Unity.Entities;

public readonly partial struct DamageAspect : IAspect
{
	public readonly Entity Entity;
	private readonly RefRW<Health> _health;
	private readonly DynamicBuffer<DamageBufferElement> _damageBuffer;
    
    
	public void ApplyDamages(EntityCommandBuffer ecb)
	{
		foreach (var damage in _damageBuffer)
		{
			_health.ValueRW.Value -= damage.Value;
            
			if (_health.ValueRW.Value <= 0 && _health.ValueRW.IsHero == false)
			{
				ecb.DestroyEntity(Entity);
			}
		}
		_damageBuffer.Clear();
	}
}


[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
[UpdateAfter(typeof(EndSimulationEntityCommandBufferSystem))]
public partial struct DamageSystem : ISystem
{
	public void OnUpdate(ref SystemState state)
	{
		var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
		var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

		foreach (var damageAspect in SystemAPI.Query<DamageAspect>())
		{
			damageAspect.ApplyDamages(ecb);
		}
	}
}