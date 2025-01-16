// Vendored from https://github.com/disas69/Unity-NTPTimeSync-Asset/blob/master/Assets/Scripts/MonoSingleton.cs
using UnityEngine;

namespace CakeLab.ARFlow.Utilities
{
    public class MonoSingleton<T> : MonoBehaviour
        where T : MonoBehaviour
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = (T)FindFirstObjectByType(typeof(T));

                    if (_instance == null)
                    {
                        _instance = new GameObject().AddComponent<T>();
                        _instance.gameObject.name = typeof(T).Name;
                    }

                    DontDestroyOnLoad(_instance.gameObject);
                }

                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
            }
        }
    }
}
