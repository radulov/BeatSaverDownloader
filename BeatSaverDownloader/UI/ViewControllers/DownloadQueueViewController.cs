using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Notify;
using BeatSaverDownloader.Misc;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace BeatSaverDownloader.UI.ViewControllers
{
    public class DownloadQueueViewController : BeatSaberMarkupLanguage.ViewControllers.BSMLResourceViewController
    {
        public override string ResourceName => "BeatSaverDownloader.UI.BSML.downloadQueue.bsml";
        internal static Action<DownloadQueueItem> didAbortDownload;
        internal static Action<DownloadQueueItem> didFinishDownloadingItem;
        [UIValue("download-queue")]
        internal List<object> queueItems = new List<object>();
        internal CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        [UIComponent("download-list")]
        private CustomCellListTableData _downloadList;

        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            base.DidDeactivate(deactivationType);
        }

        [UIAction("#post-parse")]
        internal void Setup()
        {
            //   for (int i = 0; i < 8; ++i)
            //       queueItems.Add(new DownloadQueueItem(Texture2D.whiteTexture, "SongName", "AuthorName"));
            _downloadList?.tableView?.ReloadData();
            didAbortDownload += DownloadAborted;
            didFinishDownloadingItem += UpdateDownloadingState;
        }

        internal void EnqueueSong(BeatSaverSharp.Beatmap song, Texture2D cover)
        {
            DownloadQueueItem queuedSong = new DownloadQueueItem(song, cover);
            queueItems.Add(queuedSong);
            Misc.SongDownloader.Instance.QueuedDownload(song.Hash.ToUpper());
            _downloadList?.tableView?.ReloadData();
            UpdateDownloadingState(queuedSong);
        }
        internal void AbortAllDownloads()
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = new CancellationTokenSource();
            foreach (DownloadQueueItem item in queueItems.ToArray())
            {
                item.AbortDownload();
            }
        }
        internal async void EnqueueSongs(Tuple<BeatSaverSharp.Beatmap, Texture2D>[] songs, CancellationToken cancellationToken)
        {

            for (int i = 0; i < songs.Length; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
                bool downloaded = false;
                Tuple<BeatSaverSharp.Beatmap, Texture2D> pair = songs[i];
                BeatSaverSharp.Beatmap map = pair.Item1;
                if (map.Hash.StartsWith("scoresaber"))
                {
                    downloaded = SongDownloader.Instance.IsSongDownloaded(map.Hash.Split('_')[1]);
                    if (downloaded) continue;
                    map = await BeatSaverSharp.BeatSaver.Hash(map.Hash.Split('_')[1]);
                }
                bool inQueue = queueItems.Any(x => (x as DownloadQueueItem).beatmap == map);
                downloaded = SongDownloader.Instance.IsSongDownloaded(map.Hash);
                if (!inQueue & !downloaded) EnqueueSong(map, pair.Item2);
            }
        }
        internal void UpdateDownloadingState(DownloadQueueItem item)
        {
            foreach (DownloadQueueItem inQueue in queueItems.Where(x => (x as DownloadQueueItem).queueState == SongQueueState.Queued).ToArray())
            {
                if (Misc.PluginConfig.maxSimultaneousDownloads > queueItems.Where(x => (x as DownloadQueueItem).queueState == SongQueueState.Downloading).ToArray().Length)
                    inQueue.Download();
            }
            foreach (DownloadQueueItem downloaded in queueItems.Where(x => (x as DownloadQueueItem).queueState == SongQueueState.Downloaded).ToArray())
            {
                queueItems.Remove(downloaded);
                _downloadList?.tableView?.ReloadData();
            }
            if (queueItems.Count == 0)
                SongCore.Loader.Instance.RefreshSongs(false);
        }

        internal void DownloadAborted(DownloadQueueItem download)
        {
            if (queueItems.Contains(download))
                queueItems.Remove(download);

            if (queueItems.Count == 0)
                SongCore.Loader.Instance.RefreshSongs(false);
            _downloadList?.tableView?.ReloadData();
        }
    }

    internal class DownloadQueueItem : INotifiableHost
    {
        public SongQueueState queueState = SongQueueState.Queued;
        internal Progress<double> downloadProgress;
        internal BeatSaverSharp.Beatmap beatmap;
        private UnityEngine.UI.Image _bgImage;
        private float _downloadingProgess;
        internal CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        public event PropertyChangedEventHandler PropertyChanged;

        [UIComponent("coverImage")]
        private UnityEngine.UI.RawImage _coverImage;

        [UIComponent("songNameText")]
        private TextMeshProUGUI _songNameText;

        [UIComponent("authorNameText")]
        private TextMeshProUGUI _authorNameText;

        [UIAction("abortClicked")]
        internal void AbortDownload()
        {
            cancellationTokenSource.Cancel();
            DownloadQueueViewController.didAbortDownload?.Invoke(this);
        }

        private string _songName;
        private string _authorName;
        private Texture2D _coverTexture;

        public DownloadQueueItem()
        {
        }

        public DownloadQueueItem(BeatSaverSharp.Beatmap song, Texture2D cover)
        {
            beatmap = song;
            _songName = song.Metadata.SongName;
            _coverTexture = cover;
            _authorName = $"{song.Metadata.SongAuthorName} <size=80%>[{song.Metadata.LevelAuthorName}]";
        }

        [UIAction("#post-parse")]
        internal void Setup()
        {
            if (!_coverImage || !_songNameText || !_authorNameText) return;

            var filter = _coverImage.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            filter.aspectRatio = 1f;
            filter.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.HeightControlsWidth;
            _coverImage.texture = _coverTexture;
            _coverImage.texture.wrapMode = TextureWrapMode.Clamp;
            _coverImage.rectTransform.sizeDelta = new Vector2(8, 0);
            _songNameText.text = _songName;
            _authorNameText.text = _authorName;
            downloadProgress = new Progress<double>(ProgressUpdate);

            _bgImage = _coverImage.transform.parent.gameObject.AddComponent<UnityEngine.UI.Image>();
            _bgImage.enabled = true;
            _bgImage.sprite = Sprite.Create((new Texture2D(1, 1)), new Rect(0, 0, 1, 1), Vector2.one / 2f);
            _bgImage.type = UnityEngine.UI.Image.Type.Filled;
            _bgImage.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            _bgImage.fillAmount = 0;
        }

        internal void ProgressUpdate(double progress)
        {
            _downloadingProgess = (float)progress;
            Color color = SongCore.Utilities.HSBColor.ToColor(new SongCore.Utilities.HSBColor(Mathf.PingPong(_downloadingProgess * 0.35f, 1), 1, 1));
            color.a = 0.35f;
            _bgImage.color = color;
            _bgImage.fillAmount = _downloadingProgess;
        }

        public async void Download()
        {
            queueState = SongQueueState.Downloading;
            await SongDownloader.Instance.DownloadSong(beatmap, cancellationTokenSource.Token, downloadProgress);
            queueState = SongQueueState.Downloaded;
            DownloadQueueViewController.didFinishDownloadingItem?.Invoke(this);
        }

        public void Update()
        {
        }
    }
}