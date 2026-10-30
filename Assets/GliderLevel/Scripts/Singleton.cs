using UnityEngine;

namespace yajaber
{
	public class Singleton<T> : MonoBehaviour where T : Component
	{
		private static T _instance;

		public static T Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = Object.FindFirstObjectByType<T>();
					if (_instance == null)
					{
						_instance = new GameObject(typeof(T).Name).AddComponent<T>();
					}
				}
				return _instance;
			}
		}
	}
}
