using Cysharp.Threading.Tasks;
using System.Threading;
using TMPro;
using UnityEngine;
namespace JustShooting
{
    public class AlternatingText : MonoBehaviour
    {
        [SerializeField]
        private string[] texts;
        [SerializeField]
        private TMP_Text textMeshPro;
        [SerializeField]
        private float delaySec = 5;

        private int _currentIndex;

        private void Start()
        {
            UpdateTextAsync(destroyCancellationToken).Forget();
        }

        private async UniTask UpdateTextAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                textMeshPro.text = texts[_currentIndex];
                _currentIndex = (_currentIndex + 1) % texts.Length;
                await UniTask.WaitForSeconds(delaySec, cancellationToken: ct);
            }
        }
    }
}
