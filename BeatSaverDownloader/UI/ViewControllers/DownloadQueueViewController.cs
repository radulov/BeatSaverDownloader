using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using BeatSaberMarkupLanguage.Notify;
using TMPro;
namespace BeatSaverDownloader.UI.ViewControllers
{
    public class DownloadQueueViewController : BeatSaberMarkupLanguage.ViewControllers.BSMLResourceViewController
    {
        public override string ResourceName => "BeatSaverDownloader.UI.BSML.downloadQueue.bsml";
        internal static Action<DownloadQueueItem> didAbortDownload;
        [UIValue("download-queue")]
        internal List<object> queueItems = new List<object>();
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
            _downloadList.tableView.ReloadData();
            didAbortDownload += DownloadAborted;
        }
        internal void EnqueueSong(BeatSaverSharp.Beatmap song, Texture2D cover)
        {
            queueItems.Add(new DownloadQueueItem(cover, song.Metadata.SongName, $"{song.Metadata.SongAuthorName} <size=80%>[{song.Metadata.LevelAuthorName}]"));
            _downloadList.tableView.ReloadData();
        }
        internal void DownloadAborted(DownloadQueueItem download)
        {
            if (queueItems.Contains(download))
                queueItems.Remove(download);
            _downloadList.tableView.ReloadData();
        }
    }
    internal class DownloadQueueItem : INotifiableHost
    {
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
            DownloadQueueViewController.didAbortDownload?.Invoke(this);
        }
        private string _songName;
        private string _authorName;
        private Texture2D _coverTexture;
        public DownloadQueueItem()
        {

        }
        public DownloadQueueItem(Texture2D cover, string songName, string authorName)
        {
            _songName = songName;
            _coverTexture = cover;
            _authorName = authorName;
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
            
        }


    }
}
