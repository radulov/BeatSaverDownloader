using BeatSaverDownloader.Misc;
using BeatSaverDownloader.UI;
using BS_Utils.Gameplay;
using IPA;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.SceneManagement;

namespace BeatSaverDownloader
{
    public enum SongQueueState { Queued, Downloading, Downloaded, Error };

    public class Plugin : IBeatSaberPlugin
    {
        public static Plugin instance;
        public static IPA.Logging.Logger log;
        public static BeatSaverSharp.BeatSaver BeatSaver;

        public void Init(object nullObject, IPA.Logging.Logger logger)
        {
            log = logger;
        }

        public void OnApplicationQuit()
        {
            PluginConfig.SaveConfig();
        }

        public void OnApplicationStart()
        {
            BeatSaver = new BeatSaverSharp.BeatSaver(
    new BeatSaverSharp.HttpOptions { ApplicationName = "BeatSaverDownloader", Version = Assembly.GetExecutingAssembly().GetName().Version });

            instance = this;
            PluginConfig.LoadConfig();
            Sprites.ConvertToSprites();

            PluginUI.instance.Setup();

            BS_Utils.Utilities.BSEvents.menuSceneLoadedFresh += OnMenuSceneLoadedFresh;
        }

        private void OnMenuSceneLoadedFresh()
        {
            try
            {
                PluginUI.SetupLevelDetailClone();
                Settings.SetupSettings();
                SongCore.Loader.SongsLoadedEvent += Loader_SongsLoadedEvent;
                GetUserInfo.GetUserName();
            }
            catch (Exception e)
            {
                Plugin.log.Critical("Exception on fresh menu scene change: " + e);
            }
        }

        private void Loader_SongsLoadedEvent(SongCore.Loader arg1, Dictionary<string, CustomPreviewBeatmapLevel> arg2)
        {
            if (!PluginUI.instance.moreSongsButton.Interactable)
                PluginUI.instance.moreSongsButton.Interactable = true;
        }

        public void OnUpdate()
        {
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {
        }

        public void OnSceneUnloaded(Scene scene)
        {
        }

        public void OnActiveSceneChanged(Scene prevScene, Scene nextScene)
        {
        }

        public void OnFixedUpdate()
        {
        }
    }
}