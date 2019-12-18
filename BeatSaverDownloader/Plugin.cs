using System;
using System.Reflection;
using UnityEngine.SceneManagement;
using BeatSaverDownloader.Misc;
using System.Collections.Generic;
using UnityEngine;
using BS_Utils.Gameplay;
using IPA;
using BeatSaverDownloader.UI;
namespace BeatSaverDownloader
{
    public class Plugin : IBeatSaberPlugin
    {
        public static Plugin instance;
        public static IPA.Logging.Logger log;
        public string UserAgent
        {
            get => $"BeatSaverDownloader/{Assembly.GetExecutingAssembly().GetName().Version}";
        }

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
