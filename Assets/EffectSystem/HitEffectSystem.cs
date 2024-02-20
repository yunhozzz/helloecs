using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;

[RequireMatchingQueriesForUpdate]
public partial class HitEffectSystem : SystemBase
{
    UnityEngine.ParticleSystem particleSystem;
    Transform particleSystemTransform;
    private EntityQuery _query;


    public void Init(UnityEngine.ParticleSystem particleSystem)
    {
        this.particleSystem = particleSystem;
        particleSystemTransform = particleSystem.transform;
        Enabled = true; // Everything is ready, can begin running the system
    }
    
    protected override void OnCreate()
    {
        base.OnCreate();
        
        
        _query = GetEntityQuery(ComponentType.ReadOnly<VfxEmitter>(), ComponentType.ReadOnly<LocalTransform>());
        Enabled = false; // Dont run the system until we have set everything up
    }

    protected override void OnUpdate()
    {
        Entities.ForEach((ref LocalTransform translation) =>
        {
            particleSystemTransform.position = translation.Position;
            particleSystem.Emit(1);
        }).WithoutBurst().Run();
    }
}