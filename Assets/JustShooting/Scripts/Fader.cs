using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
namespace JustShooting
{
    public class Fader : MonoBehaviour
    {
        [SerializeField]
        private Volume volume;

        private static Fader Instance { get; set; }

        private void Awake()
        {
            Instance = this;
            FadeIn().Forget();
        }

        private async UniTask Fade(float from, float to)
        {
            volume.weight = from;
            await DOTween.To(() => volume.weight, x => volume.weight = x, to, .25f).ToUniTask();
        }

        public static async UniTask FadeIn()
        {
            if (Instance == null) return;

            await Instance.Fade(1, 0);
        }

        public static async UniTask FadeOut()
        {
            if (Instance == null) return;

            await Instance.Fade(0, 1);
        }
    }
}
