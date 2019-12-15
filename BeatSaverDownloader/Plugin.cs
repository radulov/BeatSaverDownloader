using System;
using System.Reflection;
using UnityEngine.SceneManagement;
using BeatSaverDownloader.Misc;
using System.Collections.Generic;
using UnityEngine;
using BS_Utils.Gameplay;
using IPA;

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

           BS_Utils.Utilities.BSEvents.menuSceneLoadedFresh += OnMenuSceneLoadedFresh;
        }

        private void OnMenuSceneLoadedFresh()
        {
            try
            {

                GetUserInfo.GetUserName();
            }
            catch (Exception e)
            {
                Plugin.log.Critical("Exception on fresh menu scene change: " + e);
            }
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
