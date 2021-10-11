using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Disco.Audio
{
    public static class AudioLoader
    {
        public static async Task<AudioClip> TryLoadAsync(string filePath, AudioType fileType)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException(filePath);

            using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(new Uri(filePath), fileType);
            www.SendWebRequest();

            while (!www.isDone)
            {
                await Task.Delay(5);
                if (www.error != null || www.isHttpError || www.isNetworkError)
                    throw new Exception($"WWW error: {www.error}");
            }

            var clip = DownloadHandlerAudioClip.GetContent(www);
            return clip;
        }
    }
}