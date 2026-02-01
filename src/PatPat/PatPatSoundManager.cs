using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class PatPatSoundManager 
{

    private AudioClip[] _sounds = new AudioClip[3];
    private Coroutine? audioLoadCoroutine;

    public void OnInitialize(string pluginFolder, PatPatPeakMod patpat)
    {
        audioLoadCoroutine = patpat.StartCoroutine(LoadAudioClipsCoroutine(Path.Combine(pluginFolder, "sounds")));
    }

    private IEnumerator LoadAudioClipsCoroutine(string folder)
    {
        _sounds = new AudioClip[3];
        for (int i = 0; i < 3; i++)
        {
            string path = Path.Combine(folder, "pat" + i + ".ogg");
            if (!File.Exists(path)) { PatPatPeakMod.ModLogger.LogWarning($"Audio file not found: {path}"); continue; }
            string url = "file://" + path;
            using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.OGGVORBIS))
            {
                yield return uwr.SendWebRequest();
                if (uwr.result != UnityWebRequest.Result.Success) { PatPatPeakMod.ModLogger.LogWarning("Audio load failed: " + uwr.error); continue; }
                var clip = DownloadHandlerAudioClip.GetContent(uwr);
                if (clip != null) _sounds[i] = clip;
            }
        }
        audioLoadCoroutine = null;
    }


    public void PlayRandomPatSoundForObject(GameObject gameObject)
    {
        if (_sounds == null || _sounds.Length == 0) return;

        int idx = UnityEngine.Random.Range(0, _sounds.Length);
        AudioClip clip = _sounds[idx];
        if (clip == null)
        {
            return;
        }

        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            PatPatPeakMod.ModLogger.LogWarning("Clip not loaded!");
            return;
        }

        AudioSource source = gameObject.GetComponent<AudioSource>();
        if (source == null)
            source = gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = 5f;
        source.maxDistance = 20f;
        source.volume = Math.Clamp(PatPatConfig.PatSoundVolume.Value, 0.0F, 1.0F);

        source.PlayOneShot(clip);
    }

}