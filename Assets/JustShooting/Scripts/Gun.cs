using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using Random = UnityEngine.Random;
namespace JustShooting
{
    public class Gun : MonoBehaviour
    {
        public struct OnShotArgs
        {
            public Target.HitType HitType;
            public Vector3 HitPosition;
            public Vector3 ShotPosition;
            public Target HitTarget;
        }

        private static readonly int BulletCountHash = Animator.StringToHash("BulletCount");
        private static readonly int TacticalReloadAnimatorHash = Animator.StringToHash("TacticalReload");
        private static readonly int ReloadAnimatorHash = Animator.StringToHash("Reload");
        private static readonly int ShootAnimatorHash = Animator.StringToHash("Shoot");
        private static readonly int TriggerAnimatorHash = Animator.StringToHash("Trigger");

        [SerializeField]
        private Transform bulletSpawnPoint;
        [SerializeField]
        private Transform aimingPoint;
        [SerializeField]
        private RecoilController recoilController;
        [SerializeField]
        private BulletEffectPool bulletEffectPool;
        [SerializeField]
        private AudioSource shootAudio;
        [SerializeField]
        private Animator animator;
        [SerializeField]
        private InputActionReference shootActionReference;
        [SerializeField]
        private InputActionReference triggerActionReference;
        [SerializeField]
        private HapticImpulsePlayer hapticPlayer;
        [SerializeField]
        private TMP_Text bulletText;
        [SerializeField]
        private float reloadTime = 1f;
        [SerializeField]
        private float tacticalReloadTime = 0.7f;
        [SerializeField]
        private float reloadInputRange = 0.7f;
        [SerializeField]
        private LayerMask targetLayer;
        [SerializeField]
        private int magazineCapacity = 17;
        [SerializeField]
        private bool autoReload = true;

        private bool _isReloadInput;
        private bool _isReloading;
        private bool _canShoot = true;
        private bool _showCrosshair = true;
        private int _bulletCount;

        public int BulletCount
        {
            get => _bulletCount;
            private set
            {
                _bulletCount = value;
                if (bulletText) bulletText.text = _bulletCount.ToString();
                if (animator) animator.SetInteger(BulletCountHash, _bulletCount);

                if (_bulletCount == 0 && AutoReload)
                {
                    DoReloadAsync().Forget();
                }
            }
        }
        public bool CanShoot
        {
            get => _canShoot;
            set
            {
                _canShoot = value;
                bulletText.gameObject.SetActive(value);
            }
        }
        public int ShotCount { get; private set; }
        public int HitCount { get; private set; }
        public int CriticalHitCount { get; private set; }
        public int TotalHitCount => HitCount + CriticalHitCount;
        public float Accuracy => (float)TotalHitCount / ShotCount;
        public bool AutoReload { get => autoReload; set => autoReload = value; }
        public bool ShowCrosshair
        {
            get => _showCrosshair;
            set
            {
                _showCrosshair = value;
                if (aimingPoint) aimingPoint.gameObject.SetActive(value);
            }
        }

        public event System.Action<OnShotArgs> OnShot;

        private void OnEnable()
        {
            var action = shootActionReference.action;
            action.performed -= OnShootPerformed;
            action.performed += OnShootPerformed;

            ClearStats();
        }

        private void OnDisable()
        {
            shootActionReference.action.performed -= OnShootPerformed;
        }

        private void Update()
        {
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                DoReloadAsync().Forget();
            }

            if (reloadInputRange < Vector3.Dot(bulletSpawnPoint.forward, Vector3.down))
            {
                if (_isReloadInput) return;
                _isReloadInput = true;

                DoReloadAsync().Forget();
            }
            else
            {
                _isReloadInput = false;
            }

            if (animator) animator.SetFloat(TriggerAnimatorHash, triggerActionReference.action.ReadValue<float>());
        }

        private void LateUpdate()
        {
            var ray = GetAimingRay();
            if (GetRayHit(ray, out var hit))
            {
                aimingPoint.SetPositionAndRotation(hit.point, Quaternion.LookRotation(hit.point - Camera.main.transform.position));
            }
            else
            {
                var pos = ray.origin + ray.direction * 20f;
                aimingPoint.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - Camera.main.transform.position));
            }
        }

        private void OnShootPerformed(InputAction.CallbackContext ctx)
        {
            Shoot();
        }

        public void ClearStats()
        {
            BulletCount = 18;
            ShotCount = 0;
            HitCount = 0;
        }

        public async UniTask DoReloadAsync()
        {
            if (_isReloading || BulletCount == magazineCapacity + 1)
            {
                return;
            }

            _isReloading = true;

            var isTacticalReload = BulletCount != 0;
            BulletCount = 0;

            // ReSharper disable once MethodHasAsyncOverload
            Announcer.Play(AnnouncementType.Reload);

            if (isTacticalReload)
            {
                if (animator)
                {
                    animator.SetTrigger(TacticalReloadAnimatorHash);
                }

                await UniTask.WaitForSeconds(tacticalReloadTime);
                BulletCount = magazineCapacity + 1;
            }
            else
            {
                if (animator)
                {
                    animator.SetTrigger(ReloadAnimatorHash);
                }

                await UniTask.WaitForSeconds(reloadTime);
                BulletCount = magazineCapacity;
            }

            _isReloading = false;
        }

        public void Shoot()
        {
            if (!CanShoot || BulletCount <= 0)
            {
                Debug.Log("Cant shoot");
                return;
            }
            --BulletCount;

            var ray = GetAimingRay();

            ++ShotCount;

            var hitResult = CheckTargetHit(ray, out var hit, out var target);
            switch (hitResult)
            {
                default:
                case Target.HitType.None:
                    break;
                case Target.HitType.Normal:
                    HitCount++;
                    break;
                case Target.HitType.Critical:
                    CriticalHitCount++;
                    break;
            }

            OnShot?.Invoke(new OnShotArgs
            {
                HitType = hitResult,
                HitPosition = hit.point,
                ShotPosition = ray.origin,
                HitTarget = target,
            });

            PlayBulletEffect(ray);

            if (recoilController)
            {
                recoilController.ApplyRecoil();
            }

            if (shootAudio)
            {
                shootAudio.pitch = Random.Range(0.9f, 1.1f);
                shootAudio.PlayOneShot(shootAudio.clip);
            }

            if (animator)
            {
                animator.SetTrigger(ShootAnimatorHash);
            }

            if (hapticPlayer)
            {
                hapticPlayer.SendHapticImpulse(1, 0.25f, 20);
            }
        }

        private Ray GetAimingRay()
        {
            return new Ray(bulletSpawnPoint.position, bulletSpawnPoint.forward);
        }

        private bool GetRayHit(Ray ray, out RaycastHit hit)
        {
            return Physics.Raycast(ray, out hit, 1000f, targetLayer);
        }

        private Target.HitType CheckTargetHit(Ray ray, out RaycastHit hit, out Target target)
        {
            if (!GetRayHit(ray, out hit))
            {
                target = null;
                return Target.HitType.None;
            }

            var hitCollider = hit.collider;
            target = hitCollider.GetComponentInParent<Target>();
            return !target ? Target.HitType.None : target.Hit(hitCollider);
        }

        private void PlayBulletEffect(Ray ray)
        {
            var bulletEffect = bulletEffectPool.Pool.Get();
            bulletEffect.OnShoot(ray);
        }
    }
}
