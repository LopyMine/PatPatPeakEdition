using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class PatPatTextureManager
{

    public Texture2D[] frames = new Texture2D[5];
    private Coroutine? framesLoadCoroutine;

    public void OnInitialize(string pluginFolder, PatPatPeakMod patpat)
    {
        framesLoadCoroutine = patpat.StartCoroutine(LoadFrameTexturesCoroutine(Path.Combine(pluginFolder, "frames")));
    }

    private IEnumerator LoadFrameTexturesCoroutine(string folder)
    {
        frames = new Texture2D[5];
        for (int i = 0; i < 5; i++)
        {
            string file = Path.Combine(folder, "frame" + i + ".png");
            if (!File.Exists(file)) { PatPatPeakMod.ModLogger.LogWarning("Frame image not found: " + file); frames[i] = null; continue; }
            string url = "file://" + file;
            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
            {
                yield return uwr.SendWebRequest();
                if (uwr.result != UnityWebRequest.Result.Success) { PatPatPeakMod.ModLogger.LogWarning("Frame load failed: " + uwr.error); frames[i] = null; continue; }
                var tex = DownloadHandlerTexture.GetContent(uwr);
                frames[i] = tex;
            }
        }
        framesLoadCoroutine = null;
    }

}