using DG.Tweening;
using TMPro;
using UnityEngine;
namespace JustShooting
{
    public class ScoreEffect : PooledBehaviourBase<ScoreEffect>
    {
        [SerializeField]
        private TMP_Text text;
        [SerializeField]
        private Gradient gradient;
        [SerializeField]
        private float minScore = 100;
        [SerializeField]
        private float maxScore = 1000;

        public void OnScore(float score, string scoreMultiplier, Vector3 pos)
        {
            transform.position = pos;
            transform.rotation = Camera.main.transform.rotation;
            text.text = $"+{score:F0} {scoreMultiplier}";
            text.color = gradient.Evaluate(Mathf.InverseLerp(minScore, maxScore, score));

            transform
                .DOLocalMoveY(transform.localPosition.y + 1f, 0.5f)
                .OnComplete(() => DOTween
                    .ToAlpha(() => text.color, x => text.color = x, 0, 0.1f)
                    .OnComplete(() => Pool.Release(this)));
        }
    }
}
