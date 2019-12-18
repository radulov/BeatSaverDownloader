using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HMUI;
using UnityEngine.UI;
using TMPro;
using BeatSaberMarkupLanguage.Notify;
namespace BeatSaverDownloader.UI.ViewControllers
{

    public class SongDetailViewController : BeatSaberMarkupLanguage.ViewControllers.BSMLResourceViewController, INotifiableHost
    {
        public override string ResourceName => "BeatSaverDownloader.UI.BSML.songDetail.bsml";

        private GameObject _levelDetails;
        private bool _detailViewSetup = false;
        private BeatSaverSharp.Beatmap _currentSong;

        private BeatmapDifficultySegmentedControlController _difficultiesSegmentedControllerClone;
        private BeatmapCharacteristicSegmentedControlController _characteristicSegmentedControllerClone;
        private TextSegmentedControl _diffSegmentedControl;
        private IconSegmentedControl _characteristicSegmentedControl;

        private BeatSaverSharp.BeatmapCharacteristic _selectedCharacteristic;
        private BeatSaverSharp.BeatmapCharacteristicDifficulty[] _currentDifficulties;

        private TextMeshProUGUI _songNameText;
        private RawImage _coverImage;

        private TextMeshProUGUI _timeText;
        private TextMeshProUGUI _bpmText;
        private TextMeshProUGUI _npsText;
        private TextMeshProUGUI _notesText;
        private TextMeshProUGUI _obstaclesText;
        private TextMeshProUGUI _bombsText;

        private bool downloadInteractable = false;
        [UIValue("downloadInteractable")]
        public bool DownloadInteractable
        {
            get => downloadInteractable;
            set
            {
                downloadInteractable = value;
                NotifyPropertyChanged();
            }
        }

        [UIAction("#post-parse")]
        internal void Setup()
        {

            (transform as RectTransform).sizeDelta = new Vector2(70, 0);
            (transform as RectTransform).anchorMin = new Vector2(0.5f, 0);
            (transform as RectTransform).anchorMax = new Vector2(0.5f, 1);

            SetupDetailView();
        }
        [UIAction("downloadPressed")]
        internal void DownloadPressed()
        {
            MoreSongsFlowCoordinator.didPressDownload?.Invoke(_currentSong);
        }
        internal void ClearData()
        {
            if(_detailViewSetup)
            {
                //Clear all the data
                _timeText.text = "--";
                _bpmText.text = "--";
                _npsText.text = "--";
                _notesText.text = "--";
                _obstaclesText.text = "--";
                _bombsText.text = "--";
                _songNameText.text = "--";
                _diffSegmentedControl.SetTexts(new string[] { });
                _characteristicSegmentedControl.SetData(new IconSegmentedControl.DataItem[] { });
            }
        }
        internal void Initialize(BeatSaverSharp.Beatmap song, Texture2D cover)
        {
            _currentSong = song;

            _songNameText.text = _currentSong.Metadata.SongName;
            if (cover != null)
                _coverImage.texture = cover;
            DownloadInteractable = !SongCore.Collections.songWithHashPresent(_currentSong.Hash.ToUpper());
            SetupCharacteristicDisplay();
            SelectedCharacteristic(_currentSong.Metadata.Characteristics[0]);
        }


        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            base.DidDeactivate(deactivationType);
        }

        internal void SetupDetailView()
        {
            _levelDetails = GameObject.Instantiate(PluginUI._levelDetailClone, gameObject.transform);
            _levelDetails.gameObject.SetActive(false);

            _characteristicSegmentedControllerClone = _levelDetails.GetComponentInChildren<BeatmapCharacteristicSegmentedControlController>();
            _characteristicSegmentedControl = CreateIconSegmentedControl(_characteristicSegmentedControllerClone.transform as RectTransform, new Vector2(0, 0), new Vector2(0, 0),
                delegate (int value) { SelectedCharacteristic(_currentSong.Metadata.Characteristics[value]); });

            _difficultiesSegmentedControllerClone = _levelDetails.GetComponentInChildren<BeatmapDifficultySegmentedControlController>();
            _diffSegmentedControl = CreateTextSegmentedControl(_difficultiesSegmentedControllerClone.transform as RectTransform, new Vector2(0, 0), new Vector2(0, 0), 
                delegate(int value) { SelectedDifficulty(_currentDifficulties[value]); }, 3.5f, 1);

            _songNameText = _levelDetails.GetComponentsInChildren<TextMeshProUGUI>().First(x => x.gameObject.name == "SongNameText");
            _coverImage = _levelDetails.transform.Find("Level").Find("CoverImage").GetComponent<RawImage>();

            _timeText = _levelDetails.GetComponentsInChildren<TextMeshProUGUI>().First(x => x.gameObject.transform.parent.name == "Time");
            _bpmText = _levelDetails.GetComponentsInChildren<TextMeshProUGUI>().First(x => x.gameObject.transform.parent.name == "BPM");
            _npsText = _levelDetails.GetComponentsInChildren<TextMeshProUGUI>().First(x => x.gameObject.transform.parent.name == "NPS");
            _notesText = _levelDetails.GetComponentsInChildren<TextMeshProUGUI>().First(x => x.gameObject.transform.parent.name == "NotesCount");
            _obstaclesText = _levelDetails.GetComponentsInChildren<TextMeshProUGUI>().First(x => x.gameObject.transform.parent.name == "ObstaclesCount");
            _bombsText = _levelDetails.GetComponentsInChildren<TextMeshProUGUI>().First(x => x.gameObject.transform.parent.name == "BombsCount");

            _timeText.text = "--";
            _bpmText.text = "--";
            _npsText.text = "--";
            _notesText.text = "--";
            _obstaclesText.text = "--";
            _bombsText.text = "--";
            _songNameText.text = "--";
            _detailViewSetup = true;
            _levelDetails.gameObject.SetActive(true);
        }


        public void SelectedDifficulty(BeatSaverSharp.BeatmapCharacteristicDifficulty difficulty)
        {
            _timeText.text = $"{Math.Floor((double)difficulty.Length / 60):N0}:{Math.Floor((double)difficulty.Length % 60):00}";
            _bpmText.text = _currentSong.Metadata.BPM.ToString();
            _npsText.text = ((float)difficulty.Notes / (float)difficulty.Length).ToString("F2");
            _notesText.text = difficulty.Notes.ToString();
            _obstaclesText.text = difficulty.Obstacles.ToString();
            _bombsText.text = difficulty.Bombs.ToString();

        }
        public void SelectedCharacteristic(BeatSaverSharp.BeatmapCharacteristic characteristic)
        {
            _selectedCharacteristic = characteristic;
            if (_diffSegmentedControl != null)
                SetupDifficultyDisplay();

        }

        internal void SetupDifficultyDisplay()
        {
            var diffs = new List<BeatSaverSharp.BeatmapCharacteristicDifficulty>();
            List<string> diffNames = new List<string>(_selectedCharacteristic.Difficulties.Keys.Where(x => _selectedCharacteristic.Difficulties[x].HasValue)).OrderBy(x => DiffOrder(x)).ToList();
            foreach (var diff in diffNames)
            {
                if(_selectedCharacteristic.Difficulties[diff].HasValue)
                diffs.Add(_selectedCharacteristic.Difficulties[diff].Value);
            }

           for(int i = 0; i < diffNames.Count; ++i)
            {
                diffNames[i] = ToDifficultyName(diffNames[i]);
            }
            _currentDifficulties = diffs.ToArray();

            _diffSegmentedControl.SetTexts(diffNames.ToArray());
            foreach (var text in _diffSegmentedControl.GetComponentsInChildren<TextMeshProUGUI>()) text.enableWordWrapping = false;

            if (diffs.Count > 0)
                _diffSegmentedControl.SelectCellWithNumber(0);
            if (_currentDifficulties != null)
                SelectedDifficulty(_currentDifficulties[0]);
        }

        void SetupCharacteristicDisplay()
        {
            List<IconSegmentedControl.DataItem> characteristics = new List<IconSegmentedControl.DataItem>();
            foreach (var c in _currentSong.Metadata.Characteristics)
            {
                BeatmapCharacteristicSO characteristic = SongCore.Loader.beatmapCharacteristicCollection.GetBeatmapCharacteristicBySerializedName(c.Name);
                if (characteristic.characteristicNameLocalizationKey == "Missing Characteristic")
                {
                    characteristics.Add(new IconSegmentedControl.DataItem(characteristic.icon, $"Missing Characteristic: {c.Name}"));
                }
                else
                    characteristics.Add(new IconSegmentedControl.DataItem(characteristic.icon, Polyglot.Localization.Get(characteristic.descriptionLocalizationKey)));
            }

            _characteristicSegmentedControl.SetData(characteristics.ToArray());
        }
        internal static string ToDifficultyName(string name)
        {
            if (name == "easy")
                return "Easy";
            else if (name == "normal")
                return "Normal";
            else if (name == "hard")
                return "Hard";
            else if (name == "expert")
                return "Expert";
            else if (name == "expertPlus")
                return "Expert+";
            else
                return "--";
        }
        internal static int DiffOrder(string name)
        {
            switch (name)
            {
                case "easy":
                    return 0;
                case "normal":
                    return 1;
                case "hard":
                    return 2;
                case "expert":
                    return 3;
                case "expertPlus":
                    return 4;
                default:
                    return 5;
            }
        }
        public static TextSegmentedControl CreateTextSegmentedControl(RectTransform parent, Vector2 anchoredPosition, Vector2 sizeDelta, Action<int> onValueChanged = null, float fontSize = 4f, float padding = 8f)
        {
            var segmentedControl = new GameObject("CustomTextSegmentedControl", typeof(RectTransform)).AddComponent<TextSegmentedControl>();
            segmentedControl.gameObject.AddComponent<HorizontalLayoutGroup>();

            TextSegmentedControlCellNew[] _segments = Resources.FindObjectsOfTypeAll<TextSegmentedControlCellNew>();

            segmentedControl.SetPrivateField("_singleCellPrefab", _segments.First(x => x.name == "HSingleTextSegmentedControlCell"));
            segmentedControl.SetPrivateField("_firstCellPrefab", _segments.First(x => x.name == "LeftTextSegmentedControlCell"));
            segmentedControl.SetPrivateField("_middleCellPrefab", _segments.Last(x => x.name == "HMiddleTextSegmentedControlCell"));
            segmentedControl.SetPrivateField("_lastCellPrefab", _segments.Last(x => x.name == "RightTextSegmentedControlCell"));

            segmentedControl.SetPrivateField("_container", Resources.FindObjectsOfTypeAll<TextSegmentedControl>().Select(x => x.GetPrivateField<object>("_container")).First(x => x != null));

            segmentedControl.transform.SetParent(parent, false);
            (segmentedControl.transform as RectTransform).anchorMax = new Vector2(0.5f, 0.5f);
            (segmentedControl.transform as RectTransform).anchorMin = new Vector2(0.5f, 0.5f);
            (segmentedControl.transform as RectTransform).anchoredPosition = anchoredPosition;
            (segmentedControl.transform as RectTransform).sizeDelta = sizeDelta;

            segmentedControl.SetPrivateField("_fontSize", fontSize);
            segmentedControl.SetPrivateField("_padding", padding);
            if (onValueChanged != null)
                segmentedControl.didSelectCellEvent += (sender, index) => { onValueChanged(index); };

            return segmentedControl;
        }

        public static IconSegmentedControl CreateIconSegmentedControl(RectTransform parent, Vector2 anchoredPosition, Vector2 sizeDelta, Action<int> onValueChanged = null)
        {
            var segmentedControl = new GameObject("CustomIconSegmentedControl", typeof(RectTransform)).AddComponent<IconSegmentedControl>();
            segmentedControl.gameObject.AddComponent<HorizontalLayoutGroup>();

            IconSegmentedControlCell[] _segments = Resources.FindObjectsOfTypeAll<IconSegmentedControlCell>();

            segmentedControl.SetPrivateField("_singleCellPrefab", _segments.First(x => x.name == "SingleIconSegmentedControlCell"));
            segmentedControl.SetPrivateField("_firstCellPrefab", _segments.First(x => x.name == "LeftIconSegmentedControlCell"));
            segmentedControl.SetPrivateField("_middleCellPrefab", _segments.First(x => x.name == "HMiddleIconSegmentedControlCell"));
            segmentedControl.SetPrivateField("_lastCellPrefab", _segments.First(x => x.name == "RightIconSegmentedControlCell"));

            segmentedControl.SetPrivateField("_container", Resources.FindObjectsOfTypeAll<IconSegmentedControl>().Select(x => x.GetPrivateField<object>("_container")).First(x => x != null));

            segmentedControl.transform.SetParent(parent, false);
            (segmentedControl.transform as RectTransform).anchorMax = new Vector2(0.5f, 0.5f);
            (segmentedControl.transform as RectTransform).anchorMin = new Vector2(0.5f, 0.5f);
            (segmentedControl.transform as RectTransform).anchoredPosition = anchoredPosition;
            (segmentedControl.transform as RectTransform).sizeDelta = sizeDelta;

            if (onValueChanged != null)
                segmentedControl.didSelectCellEvent += (sender, index) => { onValueChanged(index); };

            return segmentedControl;
        }
    }
}
