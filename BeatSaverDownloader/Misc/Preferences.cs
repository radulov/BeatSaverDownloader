using System.Linq;
using UnityEngine;

namespace BeatSaverDownloader.Misc
{
    internal class Preferences
    {
        internal static Preferences shared = new Preferences();

        private readonly MainSettingsModelSO settings;
        public Preferences()
        {
            this.settings = Resources.FindObjectsOfTypeAll<MainSettingsModelSO>().FirstOrDefault();
        }


        public float Volume
        {
            get { return settings.volume.value; }
            set { settings.volume.value = value; }
        }
    }
}
