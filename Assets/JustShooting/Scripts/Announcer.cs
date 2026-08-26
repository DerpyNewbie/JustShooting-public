using CatHut;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
namespace JustShooting
{
    public enum AnnouncementType
    {
        AreYouReady,
        ReadyGo,
        Reload,
        Replay,
        Good,
        Ok,
        Pikon,
        Excellent,
        StageClear, // COMMENTARY: 実は StageClear と TekuTekuTeku は利用してないが存在はする。ゲーム終了時と開始時に利用予定だった。
        TekuTekuTeku,
        GameOver,
        SeeYou,
        Piron,
    }

    public class Announcer : MonoBehaviour
    {
        [SerializeField]
        private AudioSource audioSource;

        // COMMENTARY: 一応ランダムな SE を取得できるように 1 - n の関係で保持していて、Reload と Excellent で 2 つ以上のクリップがランダムで取得されます。
        [SerializeField]
        private SerializableDictionary<AnnouncementType, List<AudioClip>> announcements;

        private AnnouncementType _lastAnnouncementType;

        // COMMENTARY: うおｗ static うおｗ でも楽だから OK です。
        private static Announcer Instance { get; set; }

        private void Awake()
        {
            Instance = this;
        }

        private AudioClip GetClip(AnnouncementType type)
        {
            return announcements[type][Random.Range(0, announcements[type].Count)];
        }

        private static bool ShouldOverrideLastAnnouncement(AnnouncementType type)
        {
            switch (type)
            {
                case AnnouncementType.Reload:
                case AnnouncementType.Pikon:
                case AnnouncementType.Piron:
                    return true;
                default:
                    return false;
            }
        }

        private void PlayClip(AudioClip clip)
        {
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.pitch = Random.Range(1f, 1.05f);
            audioSource.Play();
        }

        private void Impl_Play(AnnouncementType type)
        {
            // COMMENTARY: アナウンサーから同時に一つのセリフしか発さないのはこだわりポイント。被った場合は後のアナウンスがキャンセルされる方針を取っていました。今考えるとキャンセルされるべきは先のアナウンスで、逆かも。
            if (audioSource.isPlaying && !ShouldOverrideLastAnnouncement(_lastAnnouncementType)) return;
            var clip = GetClip(type);
            PlayClip(clip);
            _lastAnnouncementType = type;
        }

        private async UniTask Impl_PlayAsync(AnnouncementType type)
        {
            if (audioSource.isPlaying && !ShouldOverrideLastAnnouncement(_lastAnnouncementType)) return;
            var clip = GetClip(type);
            PlayClip(clip);
            _lastAnnouncementType = type;
            await UniTask.WhenAny(UniTask.WaitForSeconds(clip.length), UniTask.WaitUntilValueChanged(audioSource, (a) => a.clip != clip));
        }

        public static void Play(AnnouncementType type)
        {
            if (Instance == null) return;
            Instance.Impl_Play(type);
        }

        public static async UniTask PlayAsync(AnnouncementType type)
        {
            if (Instance == null) return;
            await Instance.Impl_PlayAsync(type);
        }
    }
}
