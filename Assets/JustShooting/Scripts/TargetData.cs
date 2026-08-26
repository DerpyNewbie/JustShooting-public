using UnityEngine;

namespace JustShooting
{
    [CreateAssetMenu(fileName = "TargetData", menuName = "Scriptable Objects/TargetData")]
    public class TargetData : ScriptableObject
    {
        [SerializeField]
        private float maxHp;
        [SerializeField]
        private Material material;
        [SerializeField]
        private Color healthBarColor;
        [SerializeField]
        private float damage = 50;
        [SerializeField]
        private float criticalMultiplier = 2;

        public float MaxHealth => maxHp;
        public Material Material => material;
        public Color HealthBarColor => healthBarColor;
        public float Damage => damage;
        public float CriticalMultiplier => criticalMultiplier;
    }
}
