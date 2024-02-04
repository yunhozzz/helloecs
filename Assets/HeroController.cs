using Unity.Entities;
using UnityEngine;

namespace DefaultNamespace
{
	public class HeroController : MonoBehaviour
	{
		public void Update()
		{
			var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
			var hero = entityManager.CreateEntityQuery(typeof(HeroTag)).GetSingletonEntity();
			var heroInput = entityManager.GetComponentData<HeroInput>(hero);
			if (Input.GetKey(KeyCode.A))
				heroInput.MoveX = -1;
			else if (Input.GetKey(KeyCode.D))
				heroInput.MoveX = 1;
			else
				heroInput.MoveX = 0;
			entityManager.SetComponentData(hero, heroInput);
		}
	}
}