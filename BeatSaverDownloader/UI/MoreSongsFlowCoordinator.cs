using BeatSaberMarkupLanguage;
using BeatSaverDownloader.UI.ViewControllers;
using HMUI;
using System;
using UnityEngine;

namespace BeatSaverDownloader.UI
{
    public class MoreSongsFlowCoordinator : FlowCoordinator
    {
        private NavigationController _moreSongsNavigationcontroller;
        private MoreSongsListViewController _moreSongsView;
        private SongDetailViewController _songDetailView;
        private SongDescriptionViewController _songDescriptionView;
        private DownloadQueueViewController _downloadQueueView;

        public static Action<BeatSaverSharp.Beatmap, Texture2D> didSelectSong;
        public static Action<BeatSaverSharp.Beatmap, Texture2D> didPressDownload;
        public static Action filterDidChange;

        public void Awake()
        {
            if (_moreSongsView == null)
            {
                _moreSongsView = BeatSaberUI.CreateViewController<MoreSongsListViewController>();
                _songDetailView = BeatSaberUI.CreateViewController<SongDetailViewController>();
                _moreSongsNavigationcontroller = BeatSaberUI.CreateViewController<NavigationController>();
                _moreSongsView.navController = _moreSongsNavigationcontroller;
                _songDescriptionView = BeatSaberUI.CreateViewController<SongDescriptionViewController>();
                _downloadQueueView = BeatSaberUI.CreateViewController<DownloadQueueViewController>();

                didSelectSong += HandleDidSelectSong;
                didPressDownload += HandleDidPressDownload;
                filterDidChange += HandleFilterDidChange;
            }
        }

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            try
            {
                if (firstActivation)
                {
                    title = "More Songs";
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

        internal void HandleDidSelectSong(BeatSaverSharp.Beatmap song, Texture2D cover = null)
        {
            _songDetailView.ClearData();
            _songDescriptionView.ClearData();
            if (!_songDetailView.isInViewControllerHierarchy)
            {
                PushViewControllerToNavigationController(_moreSongsNavigationcontroller, _songDetailView);
            }
            SetRightScreenViewController(_songDescriptionView);
            _songDescriptionView.Initialize(song);
            _songDetailView.Initialize(song, cover);
        }

        internal void HandleDidPressDownload(BeatSaverSharp.Beatmap song, Texture2D cover)
        {
            Plugin.log.Info("Download pressed for song: " + song.Metadata.SongName);
            //    Misc.SongDownloader.Instance.DownloadSong(song);
            Misc.SongDownloader.Instance.QueuedDownload(song.Hash.ToUpper());
            _songDetailView.UpdateDownloadButtonStatus();
            _downloadQueueView.EnqueueSong(song, cover);
        }

        internal void HandleFilterDidChange()
        {
            if (_songDetailView.isInViewControllerHierarchy)
            {
                PopViewControllersFromNavigationController(_moreSongsNavigationcontroller, 1);
            }
            _songDetailView.ClearData();
            _songDescriptionView.ClearData();
        }
        protected override void BackButtonWasPressed(ViewController topViewController)
        {

            _moreSongsView.parserParams?.EmitEvent("closeAllModals");
            var mainFlow = BeatSaberMarkupLanguage.BeatSaberUI.MainFlowCoordinator;
            mainFlow.InvokeMethod("DismissFlowCoordinator", this, null, false);
        }
    }
}