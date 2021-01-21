using UnityEngine;

namespace Liminal.Platform.Experimental.Services
{
    /// <summary>
    /// A singleton used for running coroutine services from scripts that are not MonoBehaviours
    /// </summary>
    public class CoroutineService : MonoBehaviour
    {
        private static CoroutineService _instance;

        private void Awake()
        {
            if(_instance != null && _instance != this)
                Destroy(gameObject);

            _instance = this;
        }

        private void OnDestroy()
        {
            _instance = null;
        }

        public static CoroutineService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<CoroutineService>();
                }

                if (_instance == null)
                {
                    _instance = new GameObject("[CoroutineService]").AddComponent<CoroutineService>();
                }

                return _instance;
            }
        }
    }
}