using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace BeatSaverDownloader.Misc
{
    public class SongPlayer
    {
        readonly AudioSource audioSource;
        public SongPlayer(AudioSource audioSource)
        {
            this.audioSource = audioSource;
        }

        private readonly List<SongControl> _controlls = new List<SongControl>();

        public SongControl GetControl(BeatSaverSharp.Beatmap song)
        {
            SongControl result = _controlls.Where(control => control.song == song).FirstOrDefault();
            if (result == null)
            {
                result = new SongControl(song, audioSource);
                _controlls.Add(result);
            }
            return result;
        }

        public void PauseAll()
        {
            _controlls.ForEach(song => song.Pause());
        }
    }

    public class SongControl
    {
        public readonly BeatSaverSharp.Beatmap song;
        public readonly AudioSource audioSource;
        public SongControl(BeatSaverSharp.Beatmap song, AudioSource audioSource)
        {
            this.song = song;
            this.audioSource = audioSource;
        }

        public enum State
        {
            Initial,
            Downloading,
            Playing,
            Paused,
            Error
        }

        private State _state;
        public State state
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
                stateChanged?.Invoke(this);
            }
        }
        public event Action<SongControl> stateChanged;

        private string _path;
        private AudioClip _clip;
        private bool _startPlayOnLoad = true;
        private CancellationTokenSource _cancellationTokenSource;

        public void Play()
        {
            if (state == State.Initial)
            {
                StartLoading();
            }
            else if (state == State.Paused)
            {
                StartPlaying();
            }
        }

        public void Pause()
        {
            if (state == State.Playing)
            {
                state = State.Paused;
                audioSource.Pause();
            }

            _startPlayOnLoad = false;
        }

        public void Stop()
        {
            audioSource.Stop();
            _cancellationTokenSource?.Cancel();

            state = State.Initial;
        }

        private void StartLoading()
        {
            state = State.Downloading;

            SongDownloader.Instance.songDownloaded += SongDownloaded;

            _cancellationTokenSource = new CancellationTokenSource();
            Progress<double> downloadProgress = new Progress<double>(ProgressUpdate);

            Task doingWork = SongDownloader.Instance.DownloadSong(song, _cancellationTokenSource.Token, downloadProgress, true, true);
            doingWork.ContinueWith(SongLoaded);
        }

        private void StartPlaying()
        {
            state = State.Playing;

            audioSource.clip = _clip;
            audioSource.Play();

            UI.PluginUI.instance.StartCoroutine(WaitForFinish());
        }

        public IEnumerator WaitForFinish()
        {
            yield return new WaitUntil(() => audioSource.isPlaying == false);
            
            if (state == State.Playing)
            {
                state = State.Paused;
            }
        }

        private void SongLoaded(Task task)
        {
            SongDownloader.Instance.songDownloaded -= SongDownloaded;
        }

        private void ProgressUpdate(double progress)
        {
            // TODO: Add fancy progress update (animation, etc.)
        }

        void SongDownloaded(BeatSaverSharp.Beatmap song, string path)
        {
            _path = path;
            // TODO: Add to BeatSaverSharp .songFilename.
            string file = Directory.GetFiles(path, "*.egg").FirstOrDefault();

            LoadClip("file://" + file, (clip) => {
                Plugin.log.Info("Preview loaded for song: " + song.Metadata.SongName);
                _clip = clip;
                if (_startPlayOnLoad)
                {
                    StartPlaying();
                }
            });
        }

        private void LoadClip(string fileName, Action<AudioClip> onLoadingCompleted)
        {
            UI.PluginUI.instance.StartCoroutine(LoadClipCoroutine(fileName, onLoadingCompleted));
        }

        private IEnumerator LoadClipCoroutine(string file, Action<AudioClip> onLoadingCompleted)
        {
            UnityWebRequest AudioFiles = UnityWebRequestMultimedia.GetAudioClip(file, AudioType.OGGVORBIS);
            yield return AudioFiles.SendWebRequest();

            onLoadingCompleted(DownloadHandlerAudioClip.GetContent(AudioFiles));
        }
    }
}
