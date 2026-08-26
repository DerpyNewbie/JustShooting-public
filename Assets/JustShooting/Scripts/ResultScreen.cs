using TMPro;
using UnityEngine;

namespace JustShooting
{
    public class ResultScreen : MonoBehaviour
    {
        [SerializeField]
        private Color[] rankColors;
        [SerializeField]
        private TMP_Text scoreText;
        [SerializeField]
        private TMP_Text accText;
        [SerializeField]
        private TMP_Text killedText;
        [SerializeField]
        private TMP_Text detailsText;
        [SerializeField]
        private TMP_Text recordedAtText;

        public void Populate(Game.GameResult gameResult)
        {
            (Color rankColor, string rankText) = GetRankColorAndText(gameResult.Score);
            scoreText.text = $"{gameResult.Score:F0}: {rankText}";
            scoreText.color = rankColor;
            accText.text = gameResult.Accuracy.ToString("P1");
            killedText.text = gameResult.KillCount.ToString();
            detailsText.text = $"Shots: {gameResult.ShotCount}  Hits: {gameResult.HitCount}  Crits: {gameResult.CritCount}";

            if (recordedAtText) recordedAtText.text = $"{gameResult.RecordedAt:t}";
        }

        private (Color, string) GetRankColorAndText(float score)
        {
            return score switch
            {
                >= 150000 => (rankColors[5], "SSS"),
                >= 100000 => (rankColors[4], "SS"),
                >= 50000 => (rankColors[3], "S"),
                >= 10000 => (rankColors[2], "A"),
                >= 5000 => (rankColors[1], "B"),
                _ => (rankColors[0], "C"),
            };
        }
    }
}
