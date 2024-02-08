using Unity.Entities;
using UnityEngine;

namespace DefaultNamespace
{
	public class HeroController : MonoBehaviour
	{
		public GameObject testEffectPrefab;
		public void Update()
		{
			var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
			if (!entityManager.CreateEntityQuery(typeof(HeroTag)).TryGetSingletonEntity<HeroTag>(out var hero))
				return;
			var heroInput = entityManager.GetComponentData<HeroInput>(hero);
			if (Input.GetKey(KeyCode.A))
				heroInput.MoveX = -1;
			else if (Input.GetKey(KeyCode.D))
				heroInput.MoveX = 1;
			else
				heroInput.MoveX = 0;
			entityManager.SetComponentData(hero, heroInput);

			if (Input.GetKeyDown(KeyCode.Space))
			{
				var hit = Instantiate(testEffectPrefab);
				entityManager.AddComponentObject(hero, hit);
			}
		}
	}
}