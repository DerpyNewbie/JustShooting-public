using UnityEngine;
namespace JustShooting
{
    // COMMENTARY: 3日外に作ったと言っても過言ではない。ズルである。前作 LetsJustDuel!!!!!! や、NewbieExperiment, CenturionSystem などで利用したコードをそのまま流用している形。
    public class RecoilController : MonoBehaviour
    {
        [SerializeField]
        private float recoilErgonomics = 10f;
        [SerializeField]
        private float recoilRotationKickBack = 20f;
        [SerializeField]
        private float recoilRotationWiggle = 4f;
        [SerializeField]
        private float recoilTranslationKickBack = 0.005f;

        private Quaternion _recoilOffsetRot;
        private Vector3 _recoilOffsetPos;

        private void Update()
        {
            var t = 1 - Mathf.Exp(-recoilErgonomics * Time.deltaTime);
            _recoilOffsetRot = Quaternion.Lerp(_recoilOffsetRot, Quaternion.identity, t);
            _recoilOffsetPos = Vector3.Lerp(_recoilOffsetPos, Vector3.zero, t);

            transform.SetLocalPositionAndRotation(_recoilOffsetPos, _recoilOffsetRot);
        }

        public void ApplyRecoil()
        {
            var recoilRot = Quaternion.AngleAxis(-recoilRotationKickBack, Vector3.right) *
                            Quaternion.AngleAxis((Random.value * 2 - 1) * recoilRotationWiggle, Vector3.up) *
                            Quaternion.AngleAxis((Random.value * 2 - 1) * recoilRotationWiggle, Vector3.forward);
            var recoilPos = Vector3.back * recoilTranslationKickBack;

            _recoilOffsetPos += recoilPos;
            _recoilOffsetRot *= recoilRot;
        }
    }
}
