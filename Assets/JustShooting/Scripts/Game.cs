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

        // COMMENTARY: UI 関係の表示更新で使うために公開しているらしいです。UI 更新もローカル変数を ReactiveProperty にして UI に投げつけたらこれいらなくなってもっと綺麗にならんか?
        public bool Paused { get; set; }
        public float Score { get; private set; }
        public float TimeRemaining { get; private set; }
        public float Accuracy { get; private set; }

        public GameResult HighScoreResult { get; private set; }

        private CancellationTokenSource _gameCancellationTokenSource;

        private void Update()
        {
            // COMMENTARY: 前作でクロスヘアがあったほうがいいという意見があり、実装したはいいものの、的撃ちで見えちゃったら簡単すぎるという問題がでてきたので、一応オフにできるようトグルを実装しました。
            if (Keyboard.current.bKey.wasPressedThisFrame)
            {
                var nextCrosshair = !guns[0].ShowCrosshair;
                foreach (var gun in guns)
                    gun.ShowCrosshair = nextCrosshair;
                Debug.Log("Debug: Crosshair " + (nextCrosshair ? "ON" : "OFF") + "");
            }

            // COMMENTARY: 右の銃をオンオフできます。
            if (Keyboard.current.nKey.wasPressedThisFrame)
            {
                var go = guns[0].gameObject;
                go.SetActive(!go.activeSelf);
                Debug.Log($"Debug: {go.name} to {go.activeSelf}");
            }

            // COMMENTARY: 左の銃をオンオフ出来ます。片手だけでしっかり狙ってもらうのも想定していました。
            if (Keyboard.current.mKey.wasPressedThisFrame)
            {
                var go = guns[1].gameObject;
                go.SetActive(!go.activeSelf);
                Debug.Log($"Debug: {go.name} to {go.activeSelf}");
            }

            // COMMENTARY: CheckInterrupt のタイミングでポーズ出来ます。
            if (Keyboard.current.pKey.wasPressedThisFrame)
            {
                Paused = !Paused;
                Debug.Log($"Debug: paused = {paused}");
            }

            // COMMENTARY: 世界が 90 度傾きます。見ないでください。
            if (Keyboard.current.oKey.wasPressedThisFrame)
            {
                worldOrigin.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0, 0, 90));
                Debug.Log("Debug: world origin set to (0, 0, 0)");
            }

            // COMMENTARY: 動かないです。使わないほうが良い。使わないでほしい。というか見ないでほしい。このアホコードを。いや XRBodyTransformer 使って移動したほうがいいなぁってのは解ってたんですよ。ちょっとめんどいなって思っただけなんです。
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

        // COMMENTARY: ゲームの 1 サイクルがこの関数の中で完結している。シンプルで見やすいが、リトライ時の処理があまりスマートでない... (後述)
        public async UniTask RunGame(CancellationToken ct)
        {
            // COMMENTARY: 絶望的な量のローカル変数定義。nested class で一旦まとめたほうが綺麗だったかも...
            int hitCount = 0, critsCount = 0, shotCount = 0, comboCount = 0, killCount = 0, roundCount = 1;
            float lastHitTime = 0;
            bool lastShotWasHit = false;

            // COMMENTARY: これこの Action を lambda 定義する必要ありました? 関数に分けたほうがわかりやすいですよね?
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

                // COMMENTARY: 定期的に現れる InterruptsCheck() は、展示時に一時停止等行えるポイント。SandboxVR で機材トラブル時、実際にゲームがポーズされたのを見たので再現したかった。若干のこだわり。
                await InterruptsCheck();
                await Announcer.PlayAsync(AnnouncementType.Ok);

                // COMMENTARY: フェードアウト後にゲームフェーズのセットアップ。真っ暗のタイミングでしっかりやろう!
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

                // COMMENTARY: フェードイン後に若干のポーズをもたせるこだわり。
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

                    // COMMENTARY: デバッグ時によく使ってました。めちゃくちゃ便利。
                    if (Keyboard.current.uKey.wasPressedThisFrame)
                    {
                        Debug.Log("Debug: Game ending forcefully");
                        break;
                    }

                    TimeRemaining -= Time.deltaTime;

                    // COMMENTARY: 毎フレーム targets を回してるのはどうなん? というのはまぁ置いておいて、条件わかりやすくていいなぁって思います。てか関数に別けるべきだね。
                    if (targets.All(x => x.IsDead || !x.gameObject.activeInHierarchy))
                    {
                        ++roundCount;
                        TimeRemaining += 1f;

                        // COMMENTARY: なんと commentary でないコメントです。実際こんなこと書いてたらしいです。ちょけてんな。
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

                // COMMENTARY: ゲーム終了後のリザルト表示をしていくよ
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

                // COMMENTARY: ここらへんも関数に別けるべきやんな... async UniTask HandleResult() 的なね...? そもそも class 分けてしまうのもありだなぁ...?
                // COMMENTARY: 前のハイスコアと比較したいので、ハイスコア更新前に表示するよ。
                highScoreScreen.Populate(HighScoreResult);

                if (gameResult.Score >= HighScoreResult.Score)
                {
                    HighScoreResult = gameResult;
                }

                resultScreen.Populate(gameResult);

                result.SetActive(true);

                await Fader.FadeIn();

                // COMMENTARY: 安直ｩ!
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
                        // COMMENTARY: 激キショポイント。内部で RunGame().Forget() してる。いい方法ないんじゃろうか...
                        BeginGame();
                        break;
                    }
                }
            }
            catch (Exception)
            {
                // COMMENTARY: 若干のこだわり。必ずタイトルに戻る on any exception という感じ。タイトルに戻った後に rethrow する。
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
