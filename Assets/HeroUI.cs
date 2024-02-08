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

	private void Update()
	{
		var hero = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(HeroTag)).GetSingletonEntity();
		var heroHealth = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<Health>(hero);
		HealthSlider.value = heroHealth.Value / (float)heroHealth.Max;


		SpawnHitEffects();
	}

	private void SpawnHitEffects()
	{
		var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
		var query = entityManager.CreateEntityQuery(typeof(BulletMoveJob.BulletHitEffect));
		foreach(var e in query.ToEntityArray(AllocatorManager.Temp))
		{
			var effect = entityManager.GetComponentData<BulletMoveJob.BulletHitEffect>(e);
			var tr = entityManager.GetComponentData<LocalTransform>(e);
			var hitEffObj = Instantiate(hitPrefab, new Vector3(tr.Position.x, tr.Position.y, tr.Position.z), Quaternion.identity);
			entityManager.AddComponentObject(e, hitEffObj);
		}
	}
}
