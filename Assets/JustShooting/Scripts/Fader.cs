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

        // COMMENTARY: static なのなんだかなぁって感じ、時間的制約で難しかったけど Zenject とか学んでおきたいな (自作 DI モドキはしたことあるけど...)
        private static Fader Instance { get; set; }

        private void Awake()
        {
            Instance = this;
            // COMMENTARY: ゲーム起動時にも一応フェードするようになってます。細かい。
            FadeIn().Forget();
        }

        private async UniTask Fade(float from, float to)
        {
            volume.weight = from;
            await DOTween.To(() => volume.weight, x => volume.weight = x, to, .25f).ToUniTask();
        }

        // COMMENTARY: フェードインとアウトって命名分かりづらくないですか? ゲーム画面自体がブラックアウトから戻ってくるのでフェードインって名前ではあります。
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
