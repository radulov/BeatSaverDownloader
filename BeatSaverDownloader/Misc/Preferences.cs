namespace BeatSaverDownloader.Misc
{
    internal class Preferences
    {
        internal static Preferences shared = new Preferences();

        private readonly MainSettingsModelSO settings = new MainSettingsModelSO();

        public float Volume
        {
            get { return settings.volume.value; }
            set { settings.volume.value = value; }
        }
    }
}
