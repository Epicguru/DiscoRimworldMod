using System;
using UnityEngine;
using Verse;

namespace Disco.Audio
{
    public class ManagedAudioSource
    {
        public AudioSource Source;
        public AudioLowPassFilter LowPass;
        public AudioReverbFilter Reverb;

        public bool Remove = false;
        public float TargetVolume = 1f;
        public int MapId;
        public CellRect? Area;

        public Func<bool> IsPlaying;

        public bool DefaultIsPlaying()
        {
            return Source == null ? false : Source.isPlaying;
        }
    }
}
