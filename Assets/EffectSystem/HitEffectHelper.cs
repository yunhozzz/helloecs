using Unity.Entities;
using UnityEngine;

public class HitEffectHelper : MonoBehaviour
{
    public ParticleSystem particles;

    void Start()
    {
        World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<DamageSystem>().Init(particles);
    }
}