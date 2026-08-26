using DG.Tweening;
using UnityEngine;
namespace JustShooting
{
    public class Bouncing : MonoBehaviour
    {
        [SerializeField]
        private Vector3 endOffset = new Vector3(0, 10, 0);
        [SerializeField]
        private float duration = 1f;

        private void OnEnable()
        {
            transform.DOLocalMove(transform.localPosition + endOffset, duration).SetLoops(-1, LoopType.Yoyo);
        }
    }
}
