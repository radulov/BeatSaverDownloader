﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace BeatSaverDownloader.Misc
{
    public class SongDownloader : MonoBehaviour
    {
        public event Action<BeatSaverSharp.Beatmap, string> songDownloaded;

        private static SongDownloader _instance = null;

        public static SongDownloader Instance
        {
            get
            {
                if (!_instance)
                    _instance = new GameObject("SongDownloader").AddComponent<SongDownloader>();
                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }

        private HashSet<string> _alreadyDownloadedSongs;
        private static bool _extractingZip;

        public void Awake()
        {
            DontDestroyOnLoad(gameObject);

            if (!SongCore.Loader.AreSongsLoaded)
            {
                SongCore.Loader.SongsLoadedEvent += SongLoader_SongsLoadedEvent;
            }
            else
            {
                SongLoader_SongsLoadedEvent(null, SongCore.Loader.CustomLevels);
            }
        }

        private void SongLoader_SongsLoadedEvent(SongCore.Loader sender, ConcurrentDictionary<string, CustomPreviewBeatmapLevel> levels)
        {
            Plugin.log.Debug("Establishing Already Downloaded Songs");
            _alreadyDownloadedSongs = new HashSet<string>(levels.Values.Select(x => SongCore.Collections.hashForLevelID(x.levelID)));
        }

        public async Task DownloadSong(BeatSaverSharp.Beatmap song, System.Threading.CancellationToken token, IProgress<double> progress = null, bool direct = false, bool preview = false)
        {
            try
            {
                string customSongsPath = CustomLevelPathHelper.customLevelsDirectoryPath + "/";
                if (preview)
                {
                    customSongsPath = Path.GetTempPath();
                }
                if (!Directory.Exists(customSongsPath))
                {
                    Directory.CreateDirectory(customSongsPath);
                }
                var zip = await song.DownloadZip(direct, token, progress).ConfigureAwait(false); 
                Plugin.log.Info("Downloaded zip!");
                string result = await ExtractZipAsync(song, zip, customSongsPath).ConfigureAwait(false);
                songDownloaded?.Invoke(song, result);
            }
            catch (Exception e)
            {
                Plugin.log.Critical($"Unable to download! Exception: {e}");
                if (e is TaskCanceledException)
                    Plugin.log.Warn("Song Download Aborted.");
                else
                    Plugin.log.Critical("Failed to download Song!");
                if (_alreadyDownloadedSongs.Contains(song.Hash.ToUpper()))
                    _alreadyDownloadedSongs.Remove(song.Hash.ToUpper());
            }
        }

        private async Task<string> ExtractZipAsync(BeatSaverSharp.Beatmap songInfo, byte[] zip, string customSongsPath, bool overwrite = false)
        {
            Stream zipStream = new MemoryStream(zip);
            try
            {
                Plugin.log.Info("Extracting...");
                _extractingZip = true;
                ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
                string basePath = songInfo.Key + " (" + songInfo.Metadata.SongName + " - " + songInfo.Metadata.LevelAuthorName + ")";
                basePath = string.Join("", basePath.Split((Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).ToArray())));
                string path = customSongsPath + basePath;
                if (!overwrite && Directory.Exists(path))
                {
                    int pathNum = 1;
                    while (Directory.Exists(path + $" ({pathNum})")) ++pathNum;
                    path += $" ({pathNum})";
                }
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                Plugin.log.Info(path);
                await Task.Run(() =>
                {
                    foreach (var entry in archive.Entries)
                    {
                        var entryPath = Path.Combine(path, entry.Name); // Name instead of FullName for better security and because song zips don't have nested directories anyway
                        if (overwrite || !File.Exists(entryPath)) // Either we're overwriting or there's no existing file
                            entry.ExtractToFile(entryPath, overwrite);
                    }
                }).ConfigureAwait(false);
                archive.Dispose();
                return path;
            }
            catch (Exception e)
            {
                Plugin.log.Critical($"Unable to extract ZIP! Exception: {e}");
                _extractingZip = false;
                return null;
            }
            zipStream.Close();
        }

        public void QueuedDownload(string hash)
        {
            if (!Instance._alreadyDownloadedSongs.Contains(hash))
                Instance._alreadyDownloadedSongs.Add(hash);
        }

        public bool IsSongDownloaded(string hash)
        {
            return Instance._alreadyDownloadedSongs.Contains(hash.ToUpper());
        }
    }
}