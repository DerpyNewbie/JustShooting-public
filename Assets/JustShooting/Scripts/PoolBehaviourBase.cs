using UnityEngine;
using UnityEngine.Pool;
namespace JustShooting
{
    public abstract class PooledBehaviourBase<T> : MonoBehaviour where T : MonoBehaviour
    {
        public IObjectPool<T> Pool { get; set; }
    }

    public abstract class PoolBehaviourBase<T> : MonoBehaviour where T : PooledBehaviourBase<T>
    {
        [SerializeField]
        private GameObject prefab;
        [SerializeField]
        private int initialPoolSize = 32;

        private IObjectPool<T> _pool;

        public IObjectPool<T> Pool
        {
            get
            {
                _pool ??= new ObjectPool<T>(PoolCreate, PoolOnGet, PoolOnRelease, PoolOnDestroy, defaultCapacity: initialPoolSize);
                return _pool;
            }
        }

        private void PoolOnDestroy(T obj)
        {
            Destroy(obj.gameObject);
        }

        private void PoolOnRelease(T obj)
        {
            obj.gameObject.SetActive(false);
        }

        private void PoolOnGet(T obj)
        {
            obj.gameObject.SetActive(true);
        }

        private T PoolCreate()
        {
            var effect = Instantiate(prefab, transform).GetComponent<T>();
            effect.Pool = Pool;
            return effect;
        }
    }
}
