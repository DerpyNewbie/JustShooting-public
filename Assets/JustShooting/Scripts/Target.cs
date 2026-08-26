using DG.Tweening;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
namespace JustShooting
{
    public class Target : MonoBehaviour
    {
        public enum HitType
        {
            None,
            Normal,
            Critical,
        }

        [SerializeField]
        private TargetData[] targetData;
        [SerializeField]
        private UnityEvent<Target> onHit;
        [SerializeField]
        private Collider[] criticalColliders;
        [SerializeField]
        private Collider[] normalColliders;
        [SerializeField]
        private Slider healthSlider;
        [SerializeField]
        private Image healthBarImage;
        [SerializeField]
        private Renderer targetRenderer;

        private int _dataIndex;

        public float Health
        {
            get => _health;
            private set
            {
                var lastHealth = _health;
                _health = value;

                UpdateHealthBar();

                if (lastHealth > 0 && IsDead)
                {
                    transform.DORotateQuaternion(_tiltedRotation, 0.2f);
                }
            }
        }

        public TargetData TargetData => targetData[_dataIndex];
        public float MaxHealth => TargetData.MaxHealth;

        public bool IsDead => Health <= 0;
        private float _health;
        private Quaternion _initialRotation;
        private Quaternion _tiltedRotation;

        private void Awake()
        {
            _initialRotation = transform.rotation;
            _tiltedRotation = transform.rotation * Quaternion.Euler(-90, 0, 0);
        }

        private void Start()
        {
            StandUp();
        }

        private void OnEnable()
        {
            StandUp();
        }

        public void SetTargetData(int index)
        {
            _dataIndex = index % targetData.Length;
            var data = targetData[_dataIndex];

            healthBarImage.color = data.HealthBarColor;
            targetRenderer.material = data.Material;
        }

        public void StandUp()
        {
            Health = MaxHealth;
            gameObject.SetActive(true);
            transform.rotation = _tiltedRotation;
            transform.DORotateQuaternion(_initialRotation, 0.2f);
        }

        public HitType Hit(Collider hitCollider)
        {
            if (IsDead)
            {
                return HitType.None;
            }

            if (criticalColliders.Contains(hitCollider))
            {
                Health -= TargetData.Damage * TargetData.CriticalMultiplier;
                onHit?.Invoke(this);
                return HitType.Critical;
            }

            if (normalColliders.Contains(hitCollider))
            {
                Health -= TargetData.Damage;
                onHit?.Invoke(this);
                return HitType.Normal;
            }

            return HitType.None;
        }

        private void UpdateHealthBar()
        {
            if (healthSlider)
            {
                healthSlider.minValue = 0;
                healthSlider.maxValue = MaxHealth;
                healthSlider.value = _health;
                healthBarImage.color = TargetData.HealthBarColor;
            }
        }
    }
}
