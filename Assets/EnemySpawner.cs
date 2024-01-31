using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject EnemyPrefab;
    public float SpawnInterval;
}

public class EnemySpawnerBaker : Baker<EnemySpawner>
{
    public override void Bake(EnemySpawner authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new EnemySpawnerComponent()
        {
            Interval = authoring.SpawnInterval,
            Prefab = GetEntity(authoring.EnemyPrefab, TransformUsageFlags.Dynamic | TransformUsageFlags.Renderable),
        });
    }
}

public struct EnemySpawnerComponent : IComponentData
{
    public float Elapsed;
    public float Interval;
    public Entity Prefab;
}

public readonly partial struct EnemySpawnerAspect : IAspect
{
    public readonly Entity Entity;

    private readonly RefRW<LocalTransform> _transform;
    private readonly RefRW<EnemySpawnerComponent> _spawner;
    
    public void UpdateSpawn(float deltaTime, SystemState state)
    {
        _spawner.ValueRW.Elapsed += deltaTime;
        if (_spawner.ValueRW.Elapsed >= _spawner.ValueRW.Interval)
        {
            _spawner.ValueRW.Elapsed = 0;
            var spawned = state.EntityManager.Instantiate(_spawner.ValueRW.Prefab);
            state.EntityManager.SetComponentData(spawned, new MovingTag()
            {
                Value = true,
            });
            state.EntityManager.SetComponentData(spawned, new LocalTransform()
            {
                Position = _transform.ValueRW.Position,
                Rotation = _transform.ValueRW.Rotation,
                Scale = _transform.ValueRW.Scale,
            });
        }
    }
}

public partial struct EnemySpawnerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {   
        var deltaTime = SystemAPI.Time.DeltaTime;
        foreach (var spawner in SystemAPI.Query<EnemySpawnerAspect>())
        {
            spawner.UpdateSpawn(deltaTime, state);
        }
    }
}
