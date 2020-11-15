using BeatSaberMarkupLanguage;
using BeatSaverDownloader.UI.ViewControllers;
using HMUI;
using System;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace BeatSaverDownloader.UI
{
    public class MoreSongsFlowCoordinator : FlowCoordinator
    {
        public FlowCoordinator ParentFlowCoordinator { get; protected set; }
        public bool AllowFlowCoordinatorChange { get; protected set; } = true;
        private NavigationController _moreSongsNavigationcontroller;
        private MoreSongsListViewController _moreSongsView;
        private SongDetailViewController _songDetailView;
        private MultiSelectDetailViewController _multiSelectDetailView;
        private SongDescriptionViewController _songDescriptionView;
        private DownloadQueueViewController _downloadQueueView;
        private Misc.SongPlayer _songPlayer;

        public void Awake()
        {
            if (_moreSongsView == null)
            {
                _moreSongsView = BeatSaberUI.CreateViewController<MoreSongsListViewController>();
                _songDetailView = BeatSaberUI.CreateViewController<SongDetailViewController>();
                _multiSelectDetailView = BeatSaberUI.CreateViewController<MultiSelectDetailViewController>();
                _moreSongsNavigationcontroller = BeatSaberUI.CreateViewController<NavigationController>();
                _moreSongsView.navController = _moreSongsNavigationcontroller;
                _songDescriptionView = BeatSaberUI.CreateViewController<SongDescriptionViewController>();
                _downloadQueueView = BeatSaberUI.CreateViewController<DownloadQueueViewController>();

                _moreSongsView.didSelectSong += HandleDidSelectSong;
                _moreSongsView.filterDidChange += HandleFilterDidChange;
                _moreSongsView.multiSelectDidChange += HandleMultiSelectDidChange;
                _songDetailView.didPressDownload += HandleDidPressDownload;
                _songDetailView.didPressUploader += HandleDidPressUploader;
                _songDetailView.didPressPreview += HandleDidPressPreview;
                _songDetailView.setDescription += _songDescriptionView.Initialize;
                _multiSelectDetailView.multiSelectClearPressed += _moreSongsView.MultiSelectClear;
                _multiSelectDetailView.multiSelectDownloadPressed += HandleMultiSelectDownload;

                _songPlayer = new Misc.SongPlayer();
            }
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            try
            {
                if (firstActivation)
                {
                    UpdateTitle();
                    showBackButton = true;

                    SetViewControllersToNavigationController(_moreSongsNavigationcontroller, _moreSongsView);
                    ProvideInitialViewControllers(_moreSongsNavigationcontroller, _downloadQueueView);
                    //  PopViewControllerFromNavigationController(_moreSongsNavigationcontroller);
                }
                if (addedToHierarchy)
                {
                }
            }
            catch (Exception ex)
            {
                Plugin.log.Error(ex);
            }
        }

        public void SetParentFlowCoordinator(FlowCoordinator parent)
        {
            if (!AllowFlowCoordinatorChange)
                throw new InvalidOperationException("Changing the parent FlowCoordinator is not allowed on this instance.");
            ParentFlowCoordinator = parent;
        }

        internal void UpdateTitle()
        {
            string title = $"{_moreSongsView._currentFilter}";
            switch(_moreSongsView._currentFilter)
            {
                case MoreSongsListViewController.FilterMode.BeatSaver:
                    SetTitle(title + $" - {_moreSongsView._currentBeatSaverFilter}");
                    break;
                case MoreSongsListViewController.FilterMode.ScoreSaber:
                    SetTitle(title + $" - {_moreSongsView._currentScoreSaberFilter}");
                    break;
            }
        }
        internal void HandleDidSelectSong(StrongBox<BeatSaverSharp.Beatmap> song, Sprite cover = null)
        {
            _songDetailView.ClearData();
            _songDescriptionView.ClearData();
            if (!_moreSongsView.MultiSelectEnabled)
            {
                if (!_songDetailView.isInViewControllerHierarchy)
                {
                    PushViewControllerToNavigationController(_moreSongsNavigationcontroller, _songDetailView);
                }
                SetRightScreenViewController(_songDescriptionView, ViewController.AnimationType.None);
                _songDetailView.Initialize(song, cover);
            }
            else
            {
                int count = _moreSongsView._multiSelectSongs.Count;
                string grammar = count > 1 ? "Songs" : "Song";
                _multiSelectDetailView.MultiDownloadText = $"Add {count} {grammar} To Queue";
            }


        }

        internal void HandleDidPressDownload(BeatSaverSharp.Beatmap song, Sprite cover)
        {
            Plugin.log.Info("Download pressed for song: " + song.Metadata.SongName);
            //    Misc.SongDownloader.Instance.DownloadSong(song);
            _songDetailView.UpdateDownloadButtonStatus();
            _downloadQueueView.EnqueueSong(song, cover);
        }

        internal CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        internal void HandleDidPressPreview(BeatSaverSharp.Beatmap song)
        {
            Plugin.log.Info("Preview pressed for song: " + song.Metadata.SongName);

            Misc.SongDownloader.Instance.songDownloaded += Play;

            Progress<double> downloadProgress = new Progress<double>(ProgressUpdate);

            Task doingWork = Misc.SongDownloader.Instance.DownloadSong(song, cancellationTokenSource.Token, downloadProgress, true, true);
            doingWork.ContinueWith(OnWorkCompleted);
        }

        AudioSource myAudioSource = new AudioSource();

        void OnWorkCompleted(Task task)
        {

        }

        void ProgressUpdate(double task)
        {

        }

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
            myAudioSource = gameObject.AddComponent<AudioSource>();
            //myAudioSource.volume = Misc.Preferences.shared.Volume;
            // Load Clip then assign to audio source and play
            LoadClip("file://" + path + "/song.egg", (clip) => { myAudioSource.clip = clip; myAudioSource.Play(); });
        }

        public void LoadClip(string fileName, Action<AudioClip> onLoadingCompleted)
        {
            StartCoroutine(LoadClipCoroutine(fileName, onLoadingCompleted));
        }

        IEnumerator LoadClipCoroutine(string file, Action<AudioClip> onLoadingCompleted)
        {
            UnityWebRequest AudioFiles = UnityWebRequestMultimedia.GetAudioClip(file, AudioType.OGGVORBIS);
            yield return AudioFiles.SendWebRequest();

            onLoadingCompleted(DownloadHandlerAudioClip.GetContent(AudioFiles));
        }

        internal void HandleMultiSelectDownload()
        {
            _downloadQueueView.EnqueueSongs(_moreSongsView._multiSelectSongs.ToArray(), _downloadQueueView.cancellationTokenSource.Token);
            _moreSongsView.MultiSelectClear();
        }
        internal void HandleMultiSelectDidChange()
        {

            if (_moreSongsView.MultiSelectEnabled)
            {
                _songDetailView.ClearData();
                _songDescriptionView.ClearData();
                _moreSongsNavigationcontroller.PopViewControllers(_moreSongsNavigationcontroller.viewControllers.Count, null, true);
                _moreSongsNavigationcontroller.PushViewController(_moreSongsView, null, true);
                _moreSongsNavigationcontroller.PushViewController(_multiSelectDetailView, null, false);
            }
            else
            {
                _moreSongsNavigationcontroller.PopViewControllers(_moreSongsNavigationcontroller.viewControllers.Count, null, true);
                _moreSongsNavigationcontroller.PushViewController(_moreSongsView, null, true);
            }
        }
        internal void HandleDidPressUploader(BeatSaverSharp.User uploader)
        {
            Plugin.log.Info("Uploader pressed for user: " + uploader.Username);
            _moreSongsView.SortByUser(uploader);
        }
        internal void HandleFilterDidChange()
        {
            UpdateTitle();
            if (_songDetailView.isInViewControllerHierarchy)
            {
                PopViewControllersFromNavigationController(_moreSongsNavigationcontroller, 1);
            }
            _songDetailView.ClearData();
            _songDescriptionView.ClearData();
        }
        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            if (_songDetailView.isInViewControllerHierarchy)
            {
                PopViewControllersFromNavigationController(_moreSongsNavigationcontroller, 1, null, true);
            }
            _moreSongsView.Cleanup();
            _downloadQueueView.AbortAllDownloads();
            ParentFlowCoordinator.DismissFlowCoordinator(this);
        }

    }
}