using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace BeatSaverDownloader.Misc
{
    class SongPlayer
    {
        AudioSource audioSource;
        public void Init(AudioSource audioSource)
        {
            this.audioSource = audioSource;
        }

        public void Play(BeatSaverSharp.Beatmap song)
        {
            SongDownloader.Instance.songDownloaded += Play;

            Progress<double> downloadProgress = new Progress<double>(ProgressUpdate);

            Task doingWork = Misc.SongDownloader.Instance.DownloadSong(song, cancellationTokenSource.Token, downloadProgress, true, true);
            doingWork.ContinueWith(OnWorkCompleted);
        }

        void OnWorkCompleted(Task task)
        {

        }

        void ProgressUpdate(double task)
        {

        }

        internal CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        void Play(BeatSaverSharp.Beatmap song, string path)
        {
            // Unload audio clip
            //if (myAudioSource.clip != null)
            //{
            //myAudioSource.Stop();
            //AudioClip clip = myAudioSource.clip;
            //myAudioSource.clip = null;
            // clip.UnloadAudioData();
            //DestroyImmediate(clip, false); // This is important to avoid memory leak
            // }
            //myAudioSource = gameObject.AddComponent<AudioSource>();
            //myAudioSource.volume = Misc.Preferences.shared.Volume;
            // Load Clip then assign to audio source and play
            LoadClip("file://" + path + "/song.egg", (clip) => { audioSource.clip = clip; audioSource.Play(); });
        }

        public void LoadClip(string fileName, Action<AudioClip> onLoadingCompleted)
        {
            LoadClipCoroutine(fileName, onLoadingCompleted);
        }

        IEnumerator LoadClipCoroutine(string file, Action<AudioClip> onLoadingCompleted)
        {
            UnityWebRequest AudioFiles = UnityWebRequestMultimedia.GetAudioClip(file, AudioType.OGGVORBIS);
            yield return AudioFiles.SendWebRequest();

            onLoadingCompleted(DownloadHandlerAudioClip.GetContent(AudioFiles));
        }
    }

    class SongControl
    {
        BeatSaverSharp.Beatmap song;
        public void Init(BeatSaverSharp.Beatmap song)
        {
            this.song = song;
        }
    }
}
