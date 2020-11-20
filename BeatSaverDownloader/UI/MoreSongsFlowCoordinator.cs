﻿using BeatSaberMarkupLanguage;
using BeatSaverDownloader.UI.ViewControllers;
using HMUI;
using System;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Linq;

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

                AudioSource audioSource = Resources.FindObjectsOfTypeAll<AudioSource>().FirstOrDefault(); ;
                audioSource.volume = Misc.Preferences.shared.Volume;
                _songPlayer = new Misc.SongPlayer(audioSource);
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
            _songPlayer.PauseAll();
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

        internal void HandleDidPressPreview(BeatSaverSharp.Beatmap song)
        {
            Plugin.log.Info("Preview pressed for song: " + song.Metadata.SongName);

            Misc.SongControl control = _songPlayer.GetControl(song);
            control.stateChanged += _songDetailView.SongStateChanged;

            if (control.state == Misc.SongControl.State.Playing)
            {
                control.Pause();
            }
            else
            {
                control.Play();
            }
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
            _songPlayer.PauseAll();
            _moreSongsView.Cleanup();
            _downloadQueueView.AbortAllDownloads();
            ParentFlowCoordinator.DismissFlowCoordinator(this);
        }

    }
}