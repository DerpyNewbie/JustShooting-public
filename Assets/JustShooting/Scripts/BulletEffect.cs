using DG.Tweening;
using System.Threading;
using UnityEngine;
namespace JustShooting
{
    // COMMENTARY: 撃った方向に線が若干出るエフェクト。VALORANT で見てええなぁと思っていた。
    public class BulletEffect : PooledBehaviourBase<BulletEffect>
    {
        [SerializeField]
        private LineRenderer lineRenderer;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        public void OnShoot(Ray ray)
        {
            if (!_cts.IsCancellationRequested)
            {
                _cts.Cancel();
                _cts.Dispose();

                _cts = new CancellationTokenSource();
            }

            transform.position = ray.origin;

            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, transform.position + ray.direction * 2);

            lineRenderer
                .DOColor(
                    new Color2(Color.burlywood, new Color(0, 0, 0, 0)),
                    new Color2(new Color(0, 0, 0, 0), new Color(0, 0, 0, 0)),
                    0.2f
                ).SetEase(Ease.OutSine).OnComplete(() =>
                {
                    Pool.Release(this);
                });
        }
    }
}
