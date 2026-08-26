using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using System;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace JustShooting
{
    public class Game : MonoBehaviour
    {
        public struct GameResult
        {
            public DateTime RecordedAt;
            public float Score;
            public float Accuracy;
            public int KillCount;
            public int ShotCount;
            public int HitCount;
            public int CritCount;
        }

        [SerializeField]
        private ScoreEffectPool scoreEffectPool;
        [SerializeField]
        private Transform worldOrigin;
        [SerializeField]
        private Transform xrOrigin;
        [SerializeField]
        private Gun[] guns;
        [SerializeField]
        private Target[] targets;
        [SerializeField]
        private Target replayTarget;
        [SerializeField]
        private Target titleTarget;
        [SerializeField]
        private ResultScreen resultScreen;
        [SerializeField]
        private ResultScreen highScoreScreen;
        [SerializeField]
        private GameObject title;
        [SerializeField]
        private GameObject game;
        [SerializeField]
        private GameObject result;
        [SerializeField]
        private GameObject paused;

        public bool Paused { get; set; }
        public float Score { get; private set; }
        public float TimeRemaining { get; private set; }
        public float Accuracy { get; private set; }

        public GameResult HighScoreResult { get; private set; }

        private CancellationTokenSource _gameCancellationTokenSource;

        private void Update()
        {
            if (Keyboard.current.bKey.wasPressedThisFrame)
            {
                var nextCrosshair = !guns[0].ShowCrosshair;
                foreach (var gun in guns)
                    gun.ShowCrosshair = nextCrosshair;
                Debug.Log("Debug: Crosshair " + (nextCrosshair ? "ON" : "OFF") + "");
            }

            if (Keyboard.current.nKey.wasPressedThisFrame)
            {
                var go = guns[0].gameObject;
                go.SetActive(!go.activeSelf);
                Debug.Log($"Debug: {go.name} to {go.activeSelf}");
            }

            if (Keyboard.current.mKey.wasPressedThisFrame)
            {
                var go = guns[1].gameObject;
                go.SetActive(!go.activeSelf);
                Debug.Log($"Debug: {go.name} to {go.activeSelf}");
            }

            if (Keyboard.current.pKey.wasPressedThisFrame)
            {
                Paused = !Paused;
                Debug.Log($"Debug: paused = {paused}");
            }

            if (Keyboard.current.oKey.wasPressedThisFrame)
            {
                worldOrigin.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0, 0, 90));
                Debug.Log("Debug: world origin set to (0, 0, 0)");
            }

            if (Keyboard.current.iKey.wasPressedThisFrame)
            {
                var cameraTransform = Camera.main.transform;
                var cameraPos = cameraTransform.position;
                var cameraForward = cameraTransform.forward;
                var xrOriginPos = new Vector3(cameraPos.x, 0, cameraPos.y);
                var xrOriginRot = Quaternion.LookRotation(new Vector3(cameraForward.x, 0, cameraForward.z));
                worldOrigin.SetPositionAndRotation(xrOriginPos, xrOriginRot);
                Debug.Log($"Debug: world origin set to ({xrOriginPos.x}, 0, {xrOriginPos.y})");
            }
        }

        [UsedImplicitly]
        public void BeginGame()
        {
            _gameCancellationTokenSource?.Cancel();
            _gameCancellationTokenSource?.Dispose();
            _gameCancellationTokenSource = new CancellationTokenSource();

            RunGame(_gameCancellationTokenSource.Token).Forget();
        }

        private void SetGunsEnabled(bool active, Action<Gun.OnShotArgs> onShot = null)
        {
            foreach (var gun in guns)
            {
                gun.CanShoot = active;
                if (onShot != null)
                {
                    gun.OnShot -= onShot;
                    if (active)
                        gun.OnShot += onShot;
                }
                gun.ClearStats();
            }
        }

        private async UniTask InterruptsCheck()
        {
            if (Paused)
            {
                paused.SetActive(true);
                await UniTask.WaitUntil(() => !Paused, cancellationToken: _gameCancellationTokenSource.Token);
                paused.SetActive(false);
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                throw new OperationCanceledException("Escape key was pressed. aborting!");
            }
        }

        private void AddScore(int comboCount, bool wasCritical, bool wasQuickShot, Vector3 pos)
        {
            var scoreMultiplier = Mathf.Min(comboCount, 8) * (wasQuickShot ? 1.5f : 1f) * (wasCritical ? 2f : 1f);
            var score = 100 * scoreMultiplier;
            Score += score;

            var multiplierText = $"(x{scoreMultiplier}";

            if (wasCritical) multiplierText += " CRIT";
            if (wasQuickShot) multiplierText += " QS";
            multiplierText += ")";

            scoreEffectPool.Pool.Get().OnScore(score, multiplierText, pos);

            Announcer.Play(wasCritical ? AnnouncementType.Pikon : AnnouncementType.Piron);
        }

        public async UniTask RunGame(CancellationToken ct)
        {
            int hitCount = 0, critsCount = 0, shotCount = 0, comboCount = 0, killCount = 0, roundCount = 1;
            float lastHitTime = 0;
            bool lastShotWasHit = false;
            Action<Gun.OnShotArgs> onShotAction = (onShotArgs) =>
            {
                ++shotCount;

                switch (onShotArgs.HitType)
                {
                    case Target.HitType.Normal:
                    case Target.HitType.Critical:
                    {
                        ++hitCount;
                        ++comboCount;
                        if (onShotArgs.HitType == Target.HitType.Critical)
                        {
                            ++critsCount;
                        }

                        if (onShotArgs.HitTarget.IsDead)
                        {
                            ++killCount;
                        }

                        AddScore(
                            comboCount,
                            onShotArgs.HitType == Target.HitType.Critical,
                            Time.unscaledTime - lastHitTime < 0.5f && lastShotWasHit,
                            onShotArgs.HitPosition
                        );

                        lastHitTime = Time.unscaledTime;
                        lastShotWasHit = true;
                        break;
                    }
                    case Target.HitType.None:
                    default:
                    {
                        comboCount = 0;
                        lastShotWasHit = false;
                        break;
                    }
                }

                Accuracy = hitCount / (float)shotCount;
            };

            try
            {
                ct.ThrowIfCancellationRequested();

                await InterruptsCheck();
                await Announcer.PlayAsync(AnnouncementType.Ok);

                await Fader.FadeOut();
                title.SetActive(false);
                result.SetActive(false);

                SetGunsEnabled(false);

                foreach (var target in targets)
                {
                    target.SetTargetData(0);
                    target.gameObject.SetActive(false);
                }

                Score = 0;
                Accuracy = 0;
                TimeRemaining = 30;

                game.SetActive(true);
                await Fader.FadeIn();

                await UniTask.WaitForSeconds(0.5f, cancellationToken: ct);
                await InterruptsCheck();

                await Announcer.PlayAsync(AnnouncementType.AreYouReady);

                await UniTask.WaitForSeconds(0.5f, cancellationToken: ct);
                await InterruptsCheck();

                await Announcer.PlayAsync(AnnouncementType.ReadyGo);

                SetGunsEnabled(true, onShotAction);

                while (TimeRemaining >= 0)
                {
                    await InterruptsCheck();

                    if (Keyboard.current.uKey.wasPressedThisFrame)
                    {
                        Debug.Log("Debug: Game ending forcefully");
                        break;
                    }

                    TimeRemaining -= Time.deltaTime;
                    if (targets.All(x => x.IsDead || !x.gameObject.activeInHierarchy))
                    {
                        ++roundCount;
                        TimeRemaining += 1f;

                        // 最高の難易度調整 - 思ったよりちょうどよくてワロタ
                        targets[Mathf.Max(roundCount - 1, 0) % targets.Length].SetTargetData((int)Mathf.Min(((float)Mathf.Max(roundCount, 0) / targets.Length), 2.0f));

                        var nextTargets = targets.Where(x => !x.gameObject.activeInHierarchy).ToArray();

                        // Shuffle
                        for (var i = 0; i < nextTargets.Length; i++)
                        {
                            var tmp = nextTargets[i];
                            var rndIdx = Random.Range(0, nextTargets.Length);
                            nextTargets[i] = nextTargets[rndIdx];
                            nextTargets[rndIdx] = tmp;
                        }

                        foreach (var target in targets)
                        {
                            target.gameObject.SetActive(false);
                        }

                        var activeTargets = Random.Range(Mathf.Min(3, nextTargets.Length), Mathf.Min(5, nextTargets.Length));
                        for (var i = 0; i < activeTargets; i++)
                        {
                            nextTargets[i].StandUp();
                        }
                    }
                    await UniTask.Yield();
                }

                TimeRemaining = 0;
                SetGunsEnabled(false, onShotAction);

                await Announcer.PlayAsync(AnnouncementType.GameOver);

                await Fader.FadeOut();

                game.SetActive(false);

                var gameResult = new GameResult()
                {
                    RecordedAt = DateTime.Now,
                    Score = Score,
                    Accuracy = Accuracy,
                    KillCount = killCount,
                    ShotCount = shotCount,
                    HitCount = hitCount - critsCount,
                    CritCount = critsCount,
                };

                highScoreScreen.Populate(HighScoreResult);

                if (gameResult.Score >= HighScoreResult.Score)
                {
                    HighScoreResult = gameResult;
                }

                resultScreen.Populate(gameResult);

                result.SetActive(true);

                await Fader.FadeIn();

                if (Score < 100000)
                {
                    await Announcer.PlayAsync(AnnouncementType.Excellent);
                }
                else
                {
                    await Announcer.PlayAsync(AnnouncementType.Good);
                }

                SetGunsEnabled(true);

                var resultSelect = await UniTask.WhenAny(
                    UniTask.WaitForSeconds(60f, cancellationToken: ct),
                    UniTask.WaitUntil(() => replayTarget.IsDead, cancellationToken: ct),
                    UniTask.WaitUntil(() => titleTarget.IsDead, cancellationToken: ct)
                );

                SetGunsEnabled(false);

                switch (resultSelect)
                {
                    case 0:
                    case 2:
                    {
                        await Announcer.PlayAsync(AnnouncementType.SeeYou);

                        await UniTask.WaitForSeconds(0.5f, cancellationToken: ct);

                        await Fader.FadeOut();
                        highScoreScreen.Populate(HighScoreResult);

                        title.SetActive(true);
                        game.SetActive(false);
                        result.SetActive(false);
                        await Fader.FadeIn();

                        SetGunsEnabled(true);
                        break;
                    }
                    case 1:
                    {
                        await Announcer.PlayAsync(AnnouncementType.Replay);

                        highScoreScreen.Populate(HighScoreResult);
                        BeginGame();
                        break;
                    }
                }
            }
            catch (Exception)
            {
                SetGunsEnabled(false, onShotAction);
                await Fader.FadeOut();
                if (title) title.SetActive(true);
                if (game) game.SetActive(false);
                if (result) result.SetActive(false);
                await Fader.FadeIn();
                SetGunsEnabled(true);
                throw;
            }
        }
    }
}
