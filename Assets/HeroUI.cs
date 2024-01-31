using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

public class HeroUI : MonoBehaviour
{
	public Slider HealthSlider;
	private void Update()
	{
		var hero = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(HeroTag)).GetSingletonEntity();
		var heroHealth = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<HeroHealth>(hero);
		HealthSlider.value = heroHealth.Value / (float)heroHealth.Max;
	}
}
