using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.MenuButtons;
using BeatSaberMarkupLanguage;
namespace BeatSaverDownloader.UI
{
    public class PluginUI : PersistentSingleton<PluginUI>
    {
        public MenuButton moreSongsButton;
        internal MoreSongsFlowCoordinator _moreSongsFlowCooridinator;
        internal void Setup()
        {
            moreSongsButton = new MenuButton("More Songs", "Download More Songs from here!", MoreSongsButtonPressed, false);
            MenuButtons.instance.RegisterButton(moreSongsButton);
        }

        internal void MoreSongsButtonPressed()
        {
            ShowMoreSongsFlow();
        }
        internal void ShowMoreSongsFlow()
        {
            if (_moreSongsFlowCooridinator == null)
                _moreSongsFlowCooridinator = BeatSaberUI.CreateFlowCoordinator<MoreSongsFlowCoordinator>();
            BeatSaberMarkupLanguage.BeatSaberUI.MainFlowCoordinator.InvokeMethod("PresentFlowCoordinator", _moreSongsFlowCooridinator, null, false, false);
        }
    }
}
