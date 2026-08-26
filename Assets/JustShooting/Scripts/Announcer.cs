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
        StageClear,
        TekuTekuTeku,
        GameOver,
        SeeYou,
        Piron,
    }

    public class Announcer : MonoBehaviour
    {
        [SerializeField]
        private AudioSource audioSource;
        [SerializeField]
        private SerializableDictionary<AnnouncementType, List<AudioClip>> announcements;

        private AnnouncementType _lastAnnouncementType;

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
