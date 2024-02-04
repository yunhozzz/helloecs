using Unity.Entities;
using UnityEngine;

namespace DefaultNamespace
{
	public class Board : MonoBehaviour
	{
		
	}

	public class BoardBaker : Baker<Board>
	{
		public override void Bake(Board authoring)
		{
			var bounds = authoring.GetComponent<Renderer>().bounds;
			var entity = GetEntity(TransformUsageFlags.Renderable);
			AddComponent(entity, new BoardData()
			{
				Left = bounds.min.x,
				Right = bounds.max.x,
				NearLine = bounds.min.z,
				FarLine = bounds.max.z,
			});
		}
	}
    
	public struct BoardData : IComponentData
	{
		public float Left;
		public float Right;
		
		public float NearLine;
		public float FarLine;
	}
}