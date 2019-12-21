using BeatSaberMarkupLanguage;
using BeatSaverDownloader.UI.ViewControllers;
using HMUI;
using System;
using UnityEngine;
using System.Runtime.CompilerServices;
namespace BeatSaverDownloader.UI
{
    public class MoreSongsFlowCoordinator : FlowCoordinator
    {
        private NavigationController _moreSongsNavigationcontroller;
        private MoreSongsListViewController _moreSongsView;
        private SongDetailViewController _songDetailView;
        private MultiSelectDetailViewController _multiSelectDetailView;
        private SongDescriptionViewController _songDescriptionView;
        private DownloadQueueViewController _downloadQueueView;

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
                _songDetailView.setDescription += _songDescriptionView.Initialize;
                _multiSelectDetailView.multiSelectClearPressed += _moreSongsView.MultiSelectClear;
                _multiSelectDetailView.multiSelectDownloadPressed += HandleMultiSelectDownload;
            }
        }

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            try
            {
                if (firstActivation)
                {
                    UpdateTitle();
                    showBackButton = true;

                    SetViewControllerToNavigationConctroller(_moreSongsNavigationcontroller, _moreSongsView);
                    ProvideInitialViewControllers(_moreSongsNavigationcontroller, _downloadQueueView);
                    //  PopViewControllerFromNavigationController(_moreSongsNavigationcontroller);
                }
                if (activationType == ActivationType.AddedToHierarchy)
                {
                }
            }
            catch (Exception ex)
            {
                Plugin.log.Error(ex);
            }
        }
        internal void UpdateTitle()
        {
            title = $"{_moreSongsView._currentFilter}";
            switch(_moreSongsView._currentFilter)
            {
                case MoreSongsListViewController.FilterMode.BeatSaver:
                    title += $" - {_moreSongsView._currentBeatSaverFilter}";
                    break;
                case MoreSongsListViewController.FilterMode.ScoreSaber:
                    title += $" - {_moreSongsView._currentScoreSaberFilter}";
                    break;
            }
        }
        internal void HandleDidSelectSong(StrongBox<BeatSaverSharp.Beatmap> song, Texture2D cover = null)
        {
            _songDetailView.ClearData();
            _songDescriptionView.ClearData();
            if (!_moreSongsView.MultiSelectEnabled)
            {
                if (!_songDetailView.isInViewControllerHierarchy)
                {
                    PushViewControllerToNavigationController(_moreSongsNavigationcontroller, _songDetailView);
                }
                SetRightScreenViewController(_songDescriptionView);
                _songDetailView.Initialize(song, cover);
            }
            else
            {
                int count = _moreSongsView._multiSelectSongs.Count;
                string grammar = count > 1 ? "Songs" : "Song";
                _multiSelectDetailView.MultiDownloadText = $"Add {count} {grammar} To Queue";
            }


        }

        internal void HandleDidPressDownload(BeatSaverSharp.Beatmap song, Texture2D cover)
        {
            Plugin.log.Info("Download pressed for song: " + song.Metadata.SongName);
            //    Misc.SongDownloader.Instance.DownloadSong(song);
            _songDetailView.UpdateDownloadButtonStatus();
            _downloadQueueView.EnqueueSong(song, cover);
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
            var mainFlow = BeatSaberMarkupLanguage.BeatSaberUI.MainFlowCoordinator;
            mainFlow.DismissFlowCoordinator(this);
        }
    }
}