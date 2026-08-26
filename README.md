# JustShooting!!!!!! - Public Archive

[九州学生ゲーム大祭2026](https://tri-core.co.jp/event/game-festival-2026/)にて出展し、同率3位を獲得した
"JustShooting!!!!!!" のソースコードです。

プレイしたい方は、[ビルドをこちらからダウンロードできます。](https://r2.derpynewbie.dev/JustShooting_Win_1.0.7.zip)

[例の如く](https://github.com/DerpyNewbie/Pendulumer)
、ライセンス的に問題があるファイルに関しては除外しているため、元の状態でビルドし、プレイするためには該当のアセットを引っ張ってくる必要があります。

[.gitignore](./.gitignore) にて除外されたアセットの詳細を確認できるため、ご参考までに。

## Where to Look?

基本的にゲームのアセットに関しては `Assets/JustShooting/` 下にまとめてあります。 (以下 `Assets/JustSHooting/` 省略)

シーン自体は `Scenes/JustShooting.unity` の単一です。遷移はしません。 (3日なのでね...)

コメントとして `COMMENTARY: ` から始まっているものに関しては、このリポジトリを公開するにあたって、編集した部分を示しています。
First Commit まで辿ると元の記述を確認できます。 (.gitignore を除く)

### ゲームのサイクル

ゲームの開始は `Scripts/Game.cs` の `BeginGame` が担当しています。開始時に撃つターゲット (`Scripts/Target.cs`) の
UnityEvent `onHit` で呼び出している感じ。

1 ゲームサイクルが `Scripts/Game.cs` の `async UniTask RunGame()` にて全て羅列されて記述されているの、個人的にはシンプルに書けて好きなのですが、実際良いのかはわからない...

### ターゲットの難易度設定

各ターゲットの設定は ScriptableObject な `Scripts/TargetData.cs` が保持していて、なぜか `./Easy.asset`, `./Hard.asset`,
`./Normal.asset` と
`Assets/JustShooting` 直下に置いてあります。 (時間がなかったんだね...)

### 画面暗転

開始時や終了時などの暗転遷移は Singleton な `Scripts/Fader.cs` が担当していて、Post Processing で真っ黒な Volume
を用意し、Weight をいい感じに操作しているだけです。

### エフェクト系

銃から出るラインのエフェクト (言われないと気づかない) と、スコアが加算されるタイミングで的に表示されるエフェクトに関しては、一応
Object Pooling をしています。

`Scripts/BulletEffect.cs`, `Scripts/ScoreEffect.cs`, はどちらも `Scripts/PoolBehaviourBase.cs#PoolBehaviour`
を継承しており、
`Scripts/PoolBehaviourBase.cs#PoolBehaviourBase` (アホネーミング過ぎる。"BehaviourPoolBase" が実態に合っている) を継承した
`Scripts/BulletEffectPool.cs`, `Scripts/ScoreEffectPool.cs` などを `IObjectPool<T>` の実態として引っ張ってくることで、お手軽
Object Pooling をしています。