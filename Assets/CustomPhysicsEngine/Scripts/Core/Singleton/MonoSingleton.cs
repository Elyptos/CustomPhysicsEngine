using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Phys
{
    public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        protected static bool isDestroyed;

        /// <summary>
        ///     Global access point to the unique instance of this class.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    if (isDestroyed) return null;

                    _instance = FindExistingInstance() ?? CreateNewInstance();
                }
                return _instance;
            }
        }

        /// <summary>
        ///     Holds the unique instance for this class
        /// </summary>
        protected static T _instance;

        /// <summary>
        ///     Finds an existing instance of this singleton in the scene.
        /// </summary>
        private static T FindExistingInstance()
        {
            T existingInstance = FindObjectOfType<T>();

            if (existingInstance == null) return null;

            return existingInstance;
        }

        /// <summary>
        ///     If no instance of the T MonoBehaviour exists, creates a new GameObject in the scene
        ///     and adds T to it.
        /// </summary>
        private static T CreateNewInstance()
        {
            var containerGO = new GameObject("__" + typeof(T).Name + " (Singleton)");
            return containerGO.AddComponent<T>();
        }

        protected virtual void Awake()
        {
            // Initialize the singleton if the script is already in the scene in a GameObject
            if (_instance == null)
            {
                _instance = (T)this;
                //DontDestroyOnLoad(_instance.gameObject);

            }

            else if (this != _instance)
            {
                Debug.LogWarning(string.Format(
                    "Found a duplicated instance of a Singleton with type {0} in the GameObject {1}",
                    this.GetType(), this.gameObject.name));

                Destroy(gameObject);

                return;
            }
        }

        protected virtual void OnDestroy()
        {
            if (this != _instance) return;

            isDestroyed = true;
        }
    }
}

