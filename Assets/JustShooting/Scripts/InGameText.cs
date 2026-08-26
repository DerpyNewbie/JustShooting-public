using DG.Tweening;
using System;
using TMPro;
using UnityEngine;

namespace JustShooting
{
    public class InGameText : MonoBehaviour
    {
        [SerializeField]
        private Game game;
        [SerializeField]
        private CanvasGroup canvasGroup;
        [SerializeField]
        private TMP_Text scoreText;
        [SerializeField]
        private TMP_Text accText;
        [SerializeField]
        private TMP_Text timeText;

        private Vector3 _initialLocalPosition;

        private void Awake()
        {
            _initialLocalPosition = transform.localPosition;
        }

        private void OnEnable()
        {
            transform.localPosition = _initialLocalPosition + Vector3.down * 2f;
            canvasGroup.alpha = 0;
            DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 1, 1f);
            transform.DOLocalMove(_initialLocalPosition, 1f);
        }

        private void Update()
        {
            scoreText.text = $"Score: {game.Score:F0}";
            accText.text = $"Acc: {game.Accuracy:P0}";
            timeText.text = $"Time: {(game.TimeRemaining < 10 ? game.TimeRemaining.ToString("F0") : game.TimeRemaining.ToString("F1"))}s";
        }
    }
}
