using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

public class HeroUI : MonoBehaviour
{
	public Slider HealthSlider;
	public GameObject hitPrefab;
	
	private EntityQuery _heroQuery;
	private EntityQuery _bulletQuery;

	void Awake()
	{
		var manager = World.DefaultGameObjectInjectionWorld.EntityManager;
		_heroQuery = manager.CreateEntityQuery(typeof(HeroTag));
		_bulletQuery = manager.CreateEntityQuery(typeof(BulletMoveJob.BulletHitEffect));
	}

	private void Update()
	{
		if (!_heroQuery.TryGetSingletonEntity<HeroTag>(out var hero))
			return;
		
		var heroHealth = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<Health>(hero);
		HealthSlider.value = heroHealth.Value / (float)heroHealth.Max;


		SpawnHitEffects();
	}

	private void SpawnHitEffects()
	{
		var manager = World.DefaultGameObjectInjectionWorld.EntityManager;
		foreach(var e in _bulletQuery.ToEntityArray(AllocatorManager.Temp))
		{
			var effect = manager.GetComponentData<BulletMoveJob.BulletHitEffect>(e);
			var tr = manager.GetComponentData<LocalTransform>(e);
			var hitEffObj = Instantiate(hitPrefab, new Vector3(tr.Position.x, tr.Position.y, tr.Position.z), Quaternion.identity);
			manager.AddComponentObject(e, hitEffObj);
		}
	}
}
