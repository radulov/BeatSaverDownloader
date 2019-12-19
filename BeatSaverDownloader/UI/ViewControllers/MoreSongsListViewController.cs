using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using HMUI;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BeatSaverDownloader.UI.ViewControllers
{
    public class MoreSongsListViewController : BeatSaberMarkupLanguage.ViewControllers.BSMLResourceViewController
    {
        public enum FilterMode { Search, BeatSaver, ScoreSaber }
        public enum BeatSaverFilterOptions { Latest, Hot, Rating, Downloads, Plays, Uploader }
        public enum ScoreSaberFilterOptions { Trending, RecentlyRanked, Difficulty }

        private FilterMode _currentFilter = FilterMode.BeatSaver;
        private BeatSaverFilterOptions _currentBeatSaverFilter = BeatSaverFilterOptions.Hot;
        private ScoreSaberFilterOptions _currentScoreSaberFilter = ScoreSaberFilterOptions.Trending;
        private BeatSaverSharp.User _currentUploader;
        private string _currentSearch;
        private string _fetchingDetails = "";
        public override string ResourceName => "BeatSaverDownloader.UI.BSML.moreSongsList.bsml";
        internal NavigationController navController;
        internal CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        [UIParams]
        internal BeatSaberMarkupLanguage.Parser.BSMLParserParams parserParams;

        [UIComponent("list")]
        public CustomListTableData customListTableData;
        [UIComponent("sortList")]
        public CustomListTableData sortListTableData;
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
        [UIAction("sortSelect")]
        internal async void SelectedSortOption(TableView tableView, int row)
        {
            parserParams.EmitEvent("close-sortModal");
            SortFilter filter = (sortListTableData.data[row] as SortFilterCellInfo).sortFilter;
            _currentFilter = filter.Mode;
            _currentBeatSaverFilter = filter.BeatSaverOption;
            _currentScoreSaberFilter = filter.ScoreSaberOption;
            ClearData();
            MoreSongsFlowCoordinator.filterDidChange?.Invoke();
            await GetNewPage(2);
        }
        [UIAction("searchPressed")]
        internal async void SearchPressed(string text)
        {
            //   Plugin.log.Info("Search Pressed: " + text);
            _currentSearch = text;
            _currentFilter = FilterMode.Search;
            ClearData();
            MoreSongsFlowCoordinator.filterDidChange?.Invoke();
            await GetNewPage(2);
        }
        [UIAction("abortClicked")]
        internal void AbortPageFetch()
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = new CancellationTokenSource();
            SetLoading(false);
        }

        [UIAction("pageDownPressed")]
        internal async void PageDownPressed()
        {
            //Plugin.log.Info($"Number of cells {7}  visible cell last idx {customListTableData.tableView.visibleCells.Last().idx}  count {customListTableData.data.Count()}   math {customListTableData.data.Count() - customListTableData.tableView.visibleCells.Last().idx})");
            if (!(customListTableData.data.Count >= 1)) return;
            if ((customListTableData.data.Count() - customListTableData.tableView.visibleCells.Last().idx) <= 7)
            {
                await GetNewPage(2);
            }
        }

        internal void ClearData()
        {
            lastPage = 0;
            customListTableData.data.Clear();
            customListTableData.tableView.ReloadData();
            customListTableData.tableView.ScrollToCellWithIdx(0, TableViewScroller.ScrollPositionType.Beginning, false);
            _songs.Clear();
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
            Destroy(loadingSpinner.GetComponent<Touchable>());
            customListTableData.data.Clear();
            fetchProgress = new Progress<double>(ProgressUpdate);
            SetupSortOptions();
            await GetNewPage(2);

        }

        public void ProgressUpdate(double progress)
        {
            SetLoading(true, progress, _fetchingDetails);
        }

        public void SetLoading(bool value, double progress = 0, string details = "")
        {
            if (value)
            {
                parserParams.EmitEvent("open-loadingModal");
                loadingSpinner.ShowDownloadingProgress("Fetching More Songs... " + details, (float)progress);
            }
            else
            {
                parserParams.EmitEvent("close-loadingModal");
            }
        }

        public void SetupSortOptions()
        {
            sortListTableData.data.Add(new SortFilterCellInfo(new SortFilter(FilterMode.BeatSaver, BeatSaverFilterOptions.Hot), "Hot", "BeatSaver"));
            sortListTableData.data.Add(new SortFilterCellInfo(new SortFilter(FilterMode.BeatSaver, BeatSaverFilterOptions.Latest), "Latest", "BeatSaver"));
            sortListTableData.data.Add(new SortFilterCellInfo(new SortFilter(FilterMode.BeatSaver, BeatSaverFilterOptions.Rating), "Rating", "BeatSaver"));
            sortListTableData.data.Add(new SortFilterCellInfo(new SortFilter(FilterMode.ScoreSaber, default, ScoreSaberFilterOptions.Trending), "Trending", "ScoreSaber"));
            sortListTableData.data.Add(new SortFilterCellInfo(new SortFilter(FilterMode.ScoreSaber, default, ScoreSaberFilterOptions.RecentlyRanked), "Recently Ranked", "ScoreSaber"));
            sortListTableData.data.Add(new SortFilterCellInfo(new SortFilter(FilterMode.ScoreSaber, default, ScoreSaberFilterOptions.Difficulty), "Difficulty", "ScoreSaber"));
            sortListTableData.data.Add(new SortFilterCellInfo(new SortFilter(FilterMode.BeatSaver, BeatSaverFilterOptions.Downloads), "Downloads", "BeatSaver"));
            sortListTableData.data.Add(new SortFilterCellInfo(new SortFilter(FilterMode.BeatSaver, BeatSaverFilterOptions.Plays), "Plays", "BeatSaver"));
            sortListTableData.tableView.ReloadData();

        }
        internal async Task GetNewPage(uint count = 1)
        {
            if (Working) return;
            Plugin.log.Info($"Fetching {count} new page(s)");
            Working = true;
            try
            {
                switch (_currentFilter)
                {
                    case FilterMode.BeatSaver:
                        await GetPagesBeatSaver(count);
                        break;
                    //    case FilterMode.ScoreSaber:
                    //       await GetPagesScoreSaber(count);
                    //        break;
                    case FilterMode.Search:
                        await GetPagesSearch(count);
                        break;
                }
            }
            catch (Exception e)
            {
                if (e is TaskCanceledException)
                    Plugin.log.Warn("Page Fetching Aborted.");
                else
                    Plugin.log.Critical("Failed to fetch new pages! \n" + e);
            }
            Working = false;
        }
        internal async Task GetPagesBeatSaver(uint count)
        {
            for (uint i = 0; i < count; ++i)
            {
                _fetchingDetails = $"({i + 1}/{count})";
                BeatSaverSharp.Page page = null;
                switch (_currentBeatSaverFilter)
                {
                    case BeatSaverFilterOptions.Hot:
                        page = await BeatSaverSharp.BeatSaver.Hot(lastPage, cancellationTokenSource.Token, fetchProgress);
                        break;
                    case BeatSaverFilterOptions.Latest:
                        page = await BeatSaverSharp.BeatSaver.Latest(lastPage, cancellationTokenSource.Token, fetchProgress);
                        break;
                    case BeatSaverFilterOptions.Rating:
                        page = await BeatSaverSharp.BeatSaver.Rating(lastPage, cancellationTokenSource.Token, fetchProgress);
                        break;
                    case BeatSaverFilterOptions.Plays:
                        page = await BeatSaverSharp.BeatSaver.Plays(lastPage, cancellationTokenSource.Token, fetchProgress);
                        break;
                    case BeatSaverFilterOptions.Uploader:
                        page = await _currentUploader.Beatmaps(lastPage, cancellationTokenSource.Token, fetchProgress);
                        break;
                    case BeatSaverFilterOptions.Downloads:
                        page = await BeatSaverSharp.BeatSaver.Downloads(lastPage, cancellationTokenSource.Token, fetchProgress);
                        break;
                }
                if (page.Docs == null) continue;
                lastPage++;
                _songs.AddRange(page.Docs);
                foreach (var song in page.Docs)
                {
                    byte[] image = await song.FetchCoverImage();
                    Texture2D icon = Misc.Sprites.LoadTextureRaw(image);
                    customListTableData.data.Add(new CustomListTableData.CustomCellInfo(song.Name, song.Uploader.Username, icon));
                    customListTableData.tableView.ReloadData();
                }
            }
            _fetchingDetails = "";
        }
        internal async Task GetPagesSearch(uint count)
        {
            for (uint i = 0; i < count; ++i)
            {
                _fetchingDetails = $"({i + 1}/{count})";
                BeatSaverSharp.Page page = await BeatSaverSharp.BeatSaver.Search(_currentSearch, lastPage, cancellationTokenSource.Token, fetchProgress);
                if (page.Docs == null) continue;
                lastPage++;
                _songs.AddRange(page.Docs);
                foreach (var song in page.Docs)
                {
                    byte[] image = await song.FetchCoverImage();
                    Texture2D icon = Misc.Sprites.LoadTextureRaw(image);
                    customListTableData.data.Add(new CustomListTableData.CustomCellInfo(song.Name, song.Uploader.Username, icon));
                    customListTableData.tableView.ReloadData();
                }
            }
            _fetchingDetails = "";

        }

    }
    public class SortFilter
    {
        public MoreSongsListViewController.FilterMode Mode = MoreSongsListViewController.FilterMode.BeatSaver;
        public MoreSongsListViewController.BeatSaverFilterOptions BeatSaverOption = MoreSongsListViewController.BeatSaverFilterOptions.Hot;
        public MoreSongsListViewController.ScoreSaberFilterOptions ScoreSaberOption = MoreSongsListViewController.ScoreSaberFilterOptions.Trending;

        public SortFilter(MoreSongsListViewController.FilterMode mode, MoreSongsListViewController.BeatSaverFilterOptions beatSaverOption = default, MoreSongsListViewController.ScoreSaberFilterOptions scoreSaberOption = default)
        {
            Mode = mode;
            BeatSaverOption = beatSaverOption;
            ScoreSaberOption = scoreSaberOption;
        }
    }
    public class SortFilterCellInfo : CustomListTableData.CustomCellInfo
    {
        public SortFilter sortFilter;
        public SortFilterCellInfo(SortFilter filter, string text, string subtext = null, Texture2D icon = null) : base(text, subtext, icon)
        {
            sortFilter = filter;
        }
    }
}