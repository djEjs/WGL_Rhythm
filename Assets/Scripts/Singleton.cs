using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
	private static T _instance;
	private static readonly object _lock = new object();
	private static bool _applicationIsQuitting = false;

	public static T Instance
	{
		get
		{
			if (_applicationIsQuitting)
			{
				Debug.LogWarning($"[Singleton] {typeof(T)} is already destroyed on application quit. Won't create again - returning null.");
				return null;
			}

			lock (_lock)
			{
				if (_instance == null)
				{
					_instance = (T)FindFirstObjectByType(typeof(T));
				}
				return _instance;
			}
		}
	}

	public static bool TryGetInstance(out T instance)
	{
		instance = Instance;
		return instance != null;
	}

	protected virtual void Awake()
	{
		_applicationIsQuitting = false;

		if (_instance == null)
		{
			_instance = this as T;
		}
		else if (_instance != this)
		{
			Debug.LogWarning($"[Singleton] {typeof(T)} is already created. Destroying the duplicate.");
			Destroy(gameObject);
		}
	}

	protected virtual void OnApplicationQuit()
	{
		_applicationIsQuitting = true;
	}

	protected virtual void OnDestroy()
	{
		if (_instance == this)
		{
			_instance = null;
		}
	}
}
