using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

public class HeroUI : MonoBehaviour
{
	public Slider HealthSlider;
	private void Update()
	{
		var hero = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(HeroTag)).GetSingletonEntity();
		var heroHealth = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<Health>(hero);
		HealthSlider.value = heroHealth.Value / (float)heroHealth.Max;
	}
}
