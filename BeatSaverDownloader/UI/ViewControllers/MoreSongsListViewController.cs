using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using HMUI;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Linq;
using UnityEngine;
using BeatSaverDownloader.Misc;
using TMPro;
namespace BeatSaverDownloader.UI.ViewControllers
{
    public class MoreSongsListViewController : BeatSaberMarkupLanguage.ViewControllers.BSMLResourceViewController
    {
        public enum FilterMode { Search, BeatSaver, ScoreSaber }
        public enum BeatSaverFilterOptions { Latest, Hot, Rating, Downloads, Plays, Uploader }
        public enum ScoreSaberFilterOptions { Trending, Ranked, Difficulty, Qualified, Loved, Plays }

        internal FilterMode _currentFilter = FilterMode.ScoreSaber;
        private FilterMode _previousFilter = FilterMode.ScoreSaber;
        internal BeatSaverFilterOptions _currentBeatSaverFilter = BeatSaverFilterOptions.Hot;
        internal ScoreSaberFilterOptions _currentScoreSaberFilter = ScoreSaberFilterOptions.Trending;
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
        [UIComponent("sourceList")]
        public CustomListTableData sourceListTableData;
        [UIComponent("loadingModal")]
        public ModalView loadingModal;
        [UIComponent("sortModal")]
        public ModalView sortModal;
        [UIComponent("sortButton")]
        private UnityEngine.UI.Button _sortButton;
        [UIComponent("searchButton")]
        private UnityEngine.UI.Button _searchButton;
        [UIComponent("searchKeyboard")]
        private ModalKeyboard _searchKeyboard;

        private string _searchValue = "";
        [UIValue("searchValue")]
        public string SearchValue
        {
            get => _searchValue;
            set
            {
                _searchValue = value;
                NotifyPropertyChanged();
            }
        }
        private List<StrongBox<BeatSaverSharp.Beatmap>> _songs = new List<StrongBox<BeatSaverSharp.Beatmap>>();
        public List<Tuple<BeatSaverSharp.Beatmap, Texture2D>> _multiSelectSongs = new List<Tuple<BeatSaverSharp.Beatmap, Texture2D>>();
        public LoadingControl loadingSpinner;
        internal Progress<Double> fetchProgress;

        public Action<StrongBox<BeatSaverSharp.Beatmap>, Texture2D> didSelectSong;
        public Action filterDidChange;
        public Action multiSelectDidChange;
        public bool Working
        {
            get { return _working; }
            set { _working = value; _songsDownButton.interactable = !value; if (!loadingSpinner) return; SetLoading(value); }
        }

        private bool _working;
        private uint lastPage = 0;
        private bool _endOfResults = false;
        private bool _multiSelectEnabled = false;
        internal bool MultiSelectEnabled
        {
            get => _multiSelectEnabled;
            set
            {
                _multiSelectEnabled = value;
                ToggleMultiSelect(value);
            }
        }
        internal void ToggleMultiSelect(bool value)
        {
            MultiSelectClear();
            if (value)
            {
                customListTableData.tableView.selectionType = TableViewSelectionType.Multiple;
                _sortButton.interactable = false;
                _searchButton.interactable = false;
            }
            else
            {
                _sortButton.interactable = true;
                _searchButton.interactable = true;
                customListTableData.tableView.selectionType = TableViewSelectionType.Single;
            }

        }
        internal void MultiSelectClear()
        {
            customListTableData.tableView.ClearSelection();
            _multiSelectSongs.Clear();
        }
        [UIAction("listSelect")]
        internal void Select(TableView tableView, int row)
        {
            if (MultiSelectEnabled)
                if (!_multiSelectSongs.Any(x => x.Item1 == _songs[row].Value))
                    _multiSelectSongs.Add(_songs[row].Value, customListTableData.data[row].icon);
            didSelectSong?.Invoke(_songs[row], customListTableData.data[row].icon);
        }

        internal void SortClosed()
        {
            Plugin.log.Info("Sort modal closed");
            _currentFilter = _previousFilter;
        }

        [UIAction("sortPressed")]
        internal void SortPressed()
        {
            sourceListTableData.tableView.ClearSelection();
            sortListTableData.tableView.ClearSelection();
        }
        [UIAction("sortSelect")]
        internal async void SelectedSortOption(TableView tableView, int row)
        {
            SortFilter filter = (sortListTableData.data[row] as SortFilterCellInfo).sortFilter;
            _currentFilter = filter.Mode;
            _previousFilter = filter.Mode;
            _currentBeatSaverFilter = filter.BeatSaverOption;
            _currentScoreSaberFilter = filter.ScoreSaberOption;
            parserParams.EmitEvent("close-sortModal");
            ClearData();
            filterDidChange?.Invoke();
            await GetNewPage(3);
        }
        [UIAction("sourceSelect")]
        internal void SelectedSource(TableView tableView, int row)
        {
            parserParams.EmitEvent("close-sourceModal");
            FilterMode filter = (sourceListTableData.data[row] as SourceCellInfo).filter;
            _previousFilter = _currentFilter;
            _currentFilter = filter;
            SetupSortOptions();
            parserParams.EmitEvent("open-sortModal");
        }
        [UIAction("searchPressed")]
        internal async void SearchPressed(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            //   Plugin.log.Info("Search Pressed: " + text);
            _currentSearch = text;
            _currentFilter = FilterMode.Search;
            ClearData();
            filterDidChange?.Invoke();
            await GetNewPage(3);

        }
        [UIAction("multiSelectToggle")]
        internal void ToggleMultiSelect()
        {
            MultiSelectEnabled = !MultiSelectEnabled;
            multiSelectDidChange?.Invoke();
        }
        [UIAction("abortClicked")]
        internal void AbortPageFetch()
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = new CancellationTokenSource();
            SetLoading(false);
        }

        internal async void SortByUser(BeatSaverSharp.User user)
        {
            _currentUploader = user;
            _currentFilter = FilterMode.BeatSaver;
            _currentBeatSaverFilter = BeatSaverFilterOptions.Uploader;
            ClearData();
            filterDidChange?.Invoke();
            await GetNewPage(3);
        }
        [UIAction("pageUpPressed")]
        internal void PageUpPressed()
        {

        }
        [UIComponent("songsPageDown")]
        private UnityEngine.UI.Button _songsDownButton;
        [UIAction("pageDownPressed")]
        internal async void PageDownPressed()
        {
            //Plugin.log.Info($"Number of cells {7}  visible cell last idx {customListTableData.tableView.visibleCells.Last().idx}  count {customListTableData.data.Count()}   math {customListTableData.data.Count() - customListTableData.tableView.visibleCells.Last().idx})");
            if (!(customListTableData.data.Count >= 1) || _endOfResults || _multiSelectEnabled) return;
            if ((customListTableData.data.Count() - customListTableData.tableView.visibleCells.Last().idx) <= 7)
            {
                await GetNewPage(4);
            }
        }

        internal void ClearData()
        {
            lastPage = 0;
            customListTableData.data.Clear();
            customListTableData.tableView.ReloadData();
            customListTableData.tableView.ScrollToCellWithIdx(0, TableViewScroller.ScrollPositionType.Beginning, false);
            _songs.Clear();
            _multiSelectSongs.Clear();
        }
        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            base.DidDeactivate(deactivationType);
        }

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            base.DidActivate(firstActivation, type);
            if (!firstActivation)
            {
                InitSongList();
            }
        }

        [UIAction("#post-parse")]
        internal void SetupList()
        {
            (transform as RectTransform).sizeDelta = new Vector2(70, 0);
            (transform as RectTransform).anchorMin = new Vector2(0.5f, 0);
            (transform as RectTransform).anchorMax = new Vector2(0.5f, 1);
            loadingSpinner = GameObject.Instantiate(Resources.FindObjectsOfTypeAll<LoadingControl>().First(), loadingModal.transform);
            Destroy(loadingSpinner.GetComponent<Touchable>());
            fetchProgress = new Progress<double>(ProgressUpdate);
            SetupSourceOptions();
            sortModal._blockerClickedEvent += SortClosed;
            KEYBOARD.KEY keyKey = new KEYBOARD.KEY(_searchKeyboard.keyboard, new Vector2(-35, 11f), "Key:", 15, 10, new Color(0.92f, 0.64f, 0));
            _searchKeyboard.keyboard.keys.Add(keyKey);
            InitSongList();

        }
        internal async void InitSongList()
        {
            await GetNewPage(3);
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
        public void SetupSourceOptions()
        {
            sourceListTableData.data.Clear();
            sourceListTableData.data.Add(new SourceCellInfo(FilterMode.BeatSaver, "BeatSaver", null, Sprites.BeatSaverIcon.texture));
            sourceListTableData.data.Add(new SourceCellInfo(FilterMode.ScoreSaber, "ScoreSaber", null, Sprites.ScoreSaberIcon.texture));
            sourceListTableData.tableView.ReloadData();
        }
        public void SetupSortOptions()
        {
            sortListTableData.data.Clear();
            switch (_currentFilter)
            {
                case FilterMode.BeatSaver:
                    sortListTableData.data.Add(new SortFilterCellInfo(new SortFilter(FilterMode.BeatSaver, BeatSaverFilterOptions.Hot), "Hot", "BeatSaver", Sprites.BeatSaverIcon.texture));
                    sortListTableData.data.Add(new SortFilterCellInfo(new SortFilter(FilterMode.BeatSaver, BeatSaverFilterOptions.Latest), "Latest", "BeatSaver", Sprites.BeatSaverIcon.texture));
                    sortListTableData.data.Add(new SortFilterCellInfo(new SortFilter(FilterMode.BeatSaver, BeatSaverFilterOptions.Rating), "Rating", "BeatSaver", Sprites.BeatSaverIcon.texture));

                    sortListTableData.data.Add(new SortFilterCellInfo(new SortFilter(FilterMode.BeatSaver, BeatSaverFilterOptions.Downloads), "Downloads", "BeatSaver", Sprites.BeatSaverIcon.texture));
                    sortListTableData.data.Add(new SortFilterCellInfo(new SortFilter(FilterMode.BeatSaver, BeatSaverFilterOptions.Plays), "Plays", "BeatSaver", Sprites.BeatSaverIcon.texture));
                    break;
                case FilterMode.ScoreSaber:
                    sortListTableData.data.Add(new SortFilterCellInfo(new SortFilter(FilterMode.ScoreSaber, default, ScoreSaberFilterOptions.Trending), "Trending", "ScoreSaber", Sprites.ScoreSaberIcon.texture));
                    sortListTableData.data.Add(new SortFilterCellInfo(new SortFilter(FilterMode.ScoreSaber, default, ScoreSaberFilterOptions.Ranked), "Ranked", "ScoreSaber", Sprites.ScoreSaberIcon.texture));
                    sortListTableData.data.Add(new SortFilterCellInfo(new SortFilter(FilterMode.ScoreSaber, default, ScoreSaberFilterOptions.Qualified), "Qualified", "ScoreSaber", Sprites.ScoreSaberIcon.texture));
                    sortListTableData.data.Add(new SortFilterCellInfo(new SortFilter(FilterMode.ScoreSaber, default, ScoreSaberFilterOptions.Loved), "Loved", "ScoreSaber", Sprites.ScoreSaberIcon.texture));
                    sortListTableData.data.Add(new SortFilterCellInfo(new SortFilter(FilterMode.ScoreSaber, default, ScoreSaberFilterOptions.Difficulty), "Difficulty", "ScoreSaber", Sprites.ScoreSaberIcon.texture));
                    sortListTableData.data.Add(new SortFilterCellInfo(new SortFilter(FilterMode.ScoreSaber, default, ScoreSaberFilterOptions.Plays), "Plays", "ScoreSaber", Sprites.ScoreSaberIcon.texture));
                    break;
            }
            sortListTableData.tableView.ReloadData();
        }
        public void Cleanup()
        {
            AbortPageFetch();
            parserParams.EmitEvent("closeAllModals");
            ClearData();
        }
        internal async Task GetNewPage(uint count = 1)
        {
            if (Working) return;
            _endOfResults = false;
            Plugin.log.Info($"Fetching {count} new page(s)");
            Working = true;
            try
            {
                switch (_currentFilter)
                {
                    case FilterMode.BeatSaver:
                        await GetPagesBeatSaver(count);
                        break;
                    case FilterMode.ScoreSaber:
                        await GetPagesScoreSaber(count);
                        break;
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
        internal async Task GetPagesScoreSaber(uint count)
        {
            List<ScoreSaberSharp.Song> newMaps = new List<ScoreSaberSharp.Song>();
            for (uint i = 0; i < count; ++i)
            {
                _fetchingDetails = $"({i + 1}/{count})";
                ScoreSaberSharp.Songs page = null;
                switch (_currentScoreSaberFilter)
                {
                    case ScoreSaberFilterOptions.Trending:
                        page = await ScoreSaberSharp.ScoreSaber.Trending(lastPage, cancellationTokenSource.Token, fetchProgress);
                        break;
                    case ScoreSaberFilterOptions.Ranked:
                        page = await ScoreSaberSharp.ScoreSaber.Ranked(lastPage, cancellationTokenSource.Token, fetchProgress);
                        break;
                    case ScoreSaberFilterOptions.Qualified:
                        page = await ScoreSaberSharp.ScoreSaber.Qualified(lastPage, cancellationTokenSource.Token, fetchProgress);
                        break;
                    case ScoreSaberFilterOptions.Loved:
                        page = await ScoreSaberSharp.ScoreSaber.Loved(lastPage, cancellationTokenSource.Token, fetchProgress);
                        break;
                    case ScoreSaberFilterOptions.Plays:
                        page = await ScoreSaberSharp.ScoreSaber.Plays(lastPage, cancellationTokenSource.Token, fetchProgress);
                        break;
                    case ScoreSaberFilterOptions.Difficulty:
                        page = await ScoreSaberSharp.ScoreSaber.Difficulty(lastPage, cancellationTokenSource.Token, fetchProgress);
                        break;

                }
                lastPage++;
                if (page.songs?.Count == 0)
                {
                    _endOfResults = true;
                }
                if (page.songs != null)
                    newMaps.AddRange(page.songs);
                if (_endOfResults) break;
            }
            foreach (var song in newMaps)
            {
                BeatSaverSharp.Beatmap fromScoreSaber = ConstructBeatmapFromScoreSaber(song);
                _songs.Add(new StrongBox<BeatSaverSharp.Beatmap>(fromScoreSaber));
                if(SongDownloader.Instance.IsSongDownloaded(song.id))
                customListTableData.data.Add(new ScoreSaberCustomSongCellInfo(song, CellDidSetImage, $"<#7F7F7F>{song.name}", song.levelAuthorName));
                else
                    customListTableData.data.Add(new ScoreSaberCustomSongCellInfo(song, CellDidSetImage, song.name, song.levelAuthorName));
                customListTableData.tableView.ReloadData();
            }
            _fetchingDetails = "";
        }
        internal async Task GetPagesBeatSaver(uint count)
        {
            List<BeatSaverSharp.Beatmap> newMaps = new List<BeatSaverSharp.Beatmap>();
            for (uint i = 0; i < count; ++i)
            {
                _fetchingDetails = $"({i + 1}/{count})";
                BeatSaverSharp.Page page = null;
                switch (_currentBeatSaverFilter)
                {
                    case BeatSaverFilterOptions.Hot:
                        page = await Plugin.BeatSaver.Hot(lastPage, cancellationTokenSource.Token, fetchProgress);
                        break;
                    case BeatSaverFilterOptions.Latest:
                        page = await Plugin.BeatSaver.Latest(lastPage, cancellationTokenSource.Token, fetchProgress);
                        break;
                    case BeatSaverFilterOptions.Rating:
                        page = await Plugin.BeatSaver.Rating(lastPage, cancellationTokenSource.Token, fetchProgress);
                        break;
                    case BeatSaverFilterOptions.Plays:
                        page = await Plugin.BeatSaver.Plays(lastPage, cancellationTokenSource.Token, fetchProgress);
                        break;
                    case BeatSaverFilterOptions.Uploader:
                        page = await _currentUploader.Beatmaps(lastPage, cancellationTokenSource.Token, fetchProgress);
                        break;
                    case BeatSaverFilterOptions.Downloads:
                        page = await Plugin.BeatSaver.Downloads(lastPage, cancellationTokenSource.Token, fetchProgress);
                        break;
                }
                lastPage++;
                if (page.TotalDocs == 0 || page.NextPage == null)
                {
                    _endOfResults = true;
                }
                if (page.Docs != null)
                    newMaps.AddRange(page.Docs);
                if (_endOfResults) break;
            }
            newMaps.ForEach(x => _songs.Add(new StrongBox<BeatSaverSharp.Beatmap>(x)));
            foreach (var song in newMaps)
            {
                if(SongDownloader.Instance.IsSongDownloaded(song.Hash))
                customListTableData.data.Add(new BeatSaverCustomSongCellInfo(song, CellDidSetImage, $"<#7F7F7F>{song.Name}", song.Uploader.Username));
                else
                    customListTableData.data.Add(new BeatSaverCustomSongCellInfo(song, CellDidSetImage, song.Name, song.Uploader.Username));
                customListTableData.tableView.ReloadData();
            }
            _fetchingDetails = "";
        }
        internal async Task GetPagesSearch(uint count)
        {
            if (_currentSearch.StartsWith("Key:"))
            {
                string key = _currentSearch.Split(':')[1];
                _fetchingDetails = $" (By Key:{key}";
                BeatSaverSharp.Beatmap keyMap = await Plugin.BeatSaver.Key(key, fetchProgress);
                if (keyMap != null)
                {
                    _songs.Add(new StrongBox<BeatSaverSharp.Beatmap>(keyMap));
                    if (SongDownloader.Instance.IsSongDownloaded(keyMap.Hash))
                        customListTableData.data.Add(new BeatSaverCustomSongCellInfo(keyMap, CellDidSetImage, $"<#7F7F7F>{keyMap.Name}", keyMap.Uploader.Username));
                    else
                        customListTableData.data.Add(new BeatSaverCustomSongCellInfo(keyMap, CellDidSetImage, keyMap.Name, keyMap.Uploader.Username));
                    customListTableData.tableView.ReloadData();
                }
                _fetchingDetails = "";
                return;
            }
            List<BeatSaverSharp.Beatmap> newMaps = new List<BeatSaverSharp.Beatmap>();
            for (uint i = 0; i < count; ++i)
            {
                _fetchingDetails = $"({i + 1}/{count})";
                BeatSaverSharp.Page page = await Plugin.BeatSaver.Search(_currentSearch, lastPage, cancellationTokenSource.Token, fetchProgress);
                lastPage++;
                if (page.TotalDocs == 0 || page.NextPage == null)
                {
                    _endOfResults = true;
                }
                if (page.Docs != null)
                    newMaps.AddRange(page.Docs);
                if (_endOfResults) break;
            }
            newMaps.ForEach(x => _songs.Add(new StrongBox<BeatSaverSharp.Beatmap>(x)));
            foreach (var song in newMaps)
            {
                if (SongDownloader.Instance.IsSongDownloaded(song.Hash))
                    customListTableData.data.Add(new BeatSaverCustomSongCellInfo(song, CellDidSetImage, $"<#7F7F7F>{song.Name}", song.Uploader.Username));
                else
                    customListTableData.data.Add(new BeatSaverCustomSongCellInfo(song, CellDidSetImage, song.Name, song.Uploader.Username));
                customListTableData.tableView.ReloadData();
            }
            _fetchingDetails = "";

        }
        public static BeatSaverSharp.Beatmap ConstructBeatmapFromScoreSaber(ScoreSaberSharp.Song song)
        {
            BeatSaverSharp.Beatmap beatSaverSong = new BeatSaverSharp.Beatmap(Plugin.BeatSaver, hash: song.id);

            return beatSaverSong;
        }
        internal void CellDidSetImage(CustomListTableData.CustomCellInfo cell)
        {
            foreach (var visibleCell in customListTableData.tableView.visibleCells)
            {
                LevelListTableCell levelCell = visibleCell as LevelListTableCell;
                if (levelCell.GetField<TextMeshProUGUI>("_songNameText")?.text == cell.text)
                {
                    customListTableData.tableView.RefreshCellsContent();
                    return;
                }

            }
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
    public class SourceCellInfo : CustomListTableData.CustomCellInfo
    {
        public MoreSongsListViewController.FilterMode filter;
        public SourceCellInfo(MoreSongsListViewController.FilterMode filter, string text, string subtext = null, Texture2D icon = null) : base(text, subtext, icon)
        {
            this.filter = filter;
        }
    }
    public class BeatSaverCustomSongCellInfo : CustomListTableData.CustomCellInfo
    {
        protected BeatSaverSharp.Beatmap _song;
        Action<CustomListTableData.CustomCellInfo> _callback;
        public BeatSaverCustomSongCellInfo(BeatSaverSharp.Beatmap song, Action<CustomListTableData.CustomCellInfo> callback, string text, string subtext = null) : base(text, subtext, null)
        {
            _song = song;
            _callback = callback;
            LoadImage();
        }
        protected async void LoadImage()
        {
            byte[] image = await _song.FetchCoverImage();
            Texture2D icon = Misc.Sprites.LoadTextureRaw(image);
            base.icon = icon;
            _callback(this);
        }
    }

    public class ScoreSaberCustomSongCellInfo : CustomListTableData.CustomCellInfo
    {
        private new ScoreSaberSharp.Song _song;
        Action<CustomListTableData.CustomCellInfo> _callback;
        public ScoreSaberCustomSongCellInfo(ScoreSaberSharp.Song song, Action<CustomListTableData.CustomCellInfo> callback, string text, string subtext = null) : base(text, subtext, null)
        {
            _song = song;
            _callback = callback;
            LoadImage();
        }
        protected async void LoadImage()
        {
            byte[] image = await _song.FetchCoverImage();
            Texture2D icon = Misc.Sprites.LoadTextureRaw(image);
            base.icon = icon;
            _callback(this);
        }
    }
}