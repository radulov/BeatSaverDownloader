using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BeatSaverDownloader.UI.ViewControllers
{
    public class MoreSongsListViewController : BeatSaberMarkupLanguage.ViewControllers.BSMLResourceViewController
    {
        public override string ResourceName => "BeatSaverDownloader.UI.BSML.moreSongsList.bsml";

        [UIParams]
        private BeatSaberMarkupLanguage.Parser.BSMLParserParams parserParams;

        [UIComponent("list")]
        public CustomListTableData customListTableData;

        [UIComponent("loadingModal")]
        public ModalView loadingModal;

        public List<BeatSaverSharp.Beatmap> _songs = new List<BeatSaverSharp.Beatmap>();
        public LoadingControl loadingSpinner;
        internal Progress<Double> fetchProgress;

        public bool Working
        {
            get { return _working; }
            set { _working = value; if (!loadingSpinner) return; SetLoading(value); }
        }

        private bool _working;
        private uint lastPage = 0;

        [UIAction("listSelect")]
        internal void Select(TableView tableView, int row)
        {
            MoreSongsFlowCoordinator.didSelectSong?.Invoke(_songs[row], customListTableData.data[row].icon);
        }

        [UIAction("pageDownPressed")]
        internal void PageDownPressed()
        {
            //Plugin.log.Info($"Number of cells {7}  visible cell last idx {customListTableData.tableView.visibleCells.Last().idx}  count {customListTableData.data.Count()}   math {customListTableData.data.Count() - customListTableData.tableView.visibleCells.Last().idx})");
            if (!(customListTableData.data.Count >= 1)) return;
            if ((customListTableData.data.Count() - customListTableData.tableView.visibleCells.Last().idx) <= 14)
            {
                GetNewPage();
            }
        }

        internal async void GetNewPage(uint count = 1)
        {
            if (Working) return;
            Plugin.log.Info($"Fetching {count} new page(s)");
            Working = true;
            for (uint i = 0; i < count; ++i)
            {
                var songs = await BeatSaverSharp.BeatSaver.Latest(lastPage, fetchProgress);
                lastPage++;
                _songs.AddRange(songs.Docs);
                foreach (var song in songs.Docs)
                {
                    byte[] image = await song.FetchCoverImage();
                    Texture2D icon = Misc.Sprites.LoadTextureRaw(image);
                    customListTableData.data.Add(new CustomListTableData.CustomCellInfo(song.Name, song.Uploader.Username, icon));
                }
                customListTableData.tableView.ReloadData();
            }

            Working = false;
        }

        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            base.DidDeactivate(deactivationType);
        }

        [UIAction("#post-parse")]
        internal async void SetupList()
        {
            (transform as RectTransform).sizeDelta = new Vector2(70, 0);
            (transform as RectTransform).anchorMin = new Vector2(0.5f, 0);
            (transform as RectTransform).anchorMax = new Vector2(0.5f, 1);
            loadingSpinner = GameObject.Instantiate(Resources.FindObjectsOfTypeAll<LoadingControl>().First(), loadingModal.transform);
            customListTableData.data.Clear();
            fetchProgress = new Progress<double>(ProgressUpdate);
            // Add items here
            GetNewPage(2);
            // customListTableData.tableView.ScrollToCellWithIdx(InitialItem, HMUI.TableViewScroller.ScrollPositionType.Beginning, false);
            // customListTableData.tableView.SelectCellWithIdx(InitialItem);
        }

        public void ProgressUpdate(double progress)
        {
            SetLoading(true, progress);
        }

        public void SetLoading(bool value, double progress = 0)
        {
            if (value)
            {
                parserParams.EmitEvent("open-loadingModal");
                loadingSpinner.ShowDownloadingProgress("Fetching More Songs...", (float)progress);
            }
            else
            {
                parserParams.EmitEvent("close-loadingModal");
            }
        }
    }
}