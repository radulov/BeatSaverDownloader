using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSaverDownloader.UI.ViewControllers
{
    public class SongDescriptionViewController : BeatSaberMarkupLanguage.ViewControllers.BSMLResourceViewController
    {
        public override string ResourceName => "BeatSaverDownloader.UI.BSML.songDescription.bsml";
        [UIValue("description")]
        private string songDescription = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Aenean at tristique purus, non malesuada lacus. Nulla rhoncus ante tortor, vitae lobortis nunc bibendum a. Mauris auctor, risus tincidunt posuere semper, neque odio rutrum nisi, vel maximus ligula dui quis urna. Aenean in tincidunt est, nec pellentesque leo. Aliquam et congue mi. Nunc at nulla eleifend, imperdiet ipsum vitae, posuere libero. Vivamus ac justo eget felis cursus consequat. Nunc porttitor augue at porttitor auctor. Nullam id turpis id diam mattis tincidunt sed semper tortor.\n Etiam ut sem eget eros euismod euismod. Donec dui odio, tempor vitae ullamcorper a, pellentesque vitae lectus. Donec tristique ligula eu ligula vehicula consectetur. Integer sollicitudin eu neque ac consectetur. Nam tempus leo non sapien bibendum, a dictum elit semper. Nam orci elit, sollicitudin eget ligula ac, vehicula imperdiet risus. Aenean diam nunc, consequat at diam sed, tincidunt facilisis dui. Morbi porta tellus ac suscipit egestas. Quisque sit amet purus ex. Aliquam maximus purus dolor, non euismod augue eleifend porttitor. Nullam tincidunt et dui tincidunt consequat. Curabitur quis tellus urna.";
        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            base.DidDeactivate(deactivationType);
        }
        internal void ClearData()
        {

        }
        internal void Initialize(BeatSaverSharp.Beatmap song)
        {

        }
        [UIAction("#post-parse")]
        internal void Setup()
        {

        }
    }
}
