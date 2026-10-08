using System;
using System.Collections.Generic;
using UnityEngine;

// Gain으로 가공한 클립의 생성·재사용·해제를 담당한다.
// 원본 AudioClip은 소유하지 않고, 생성한 복사본만 파괴한다.
internal sealed class SoundClipCache : IDisposable
{
    private readonly Dictionary<(AudioClip clip, float gain), AudioClip> _amplifiedClips =
        new Dictionary<(AudioClip clip, float gain), AudioClip>();

    public AudioClip GetClip(AudioClip clip, float gain)
    {
        if (clip == null || gain <= 1f)
        {
            return clip;
        }

        var key = (clip, gain);
        if (_amplifiedClips.TryGetValue(key, out AudioClip cached))
        {
            return cached != null ? cached : clip;
        }

        AudioClip amplified = CreateAmplifiedClip(clip, gain);
        _amplifiedClips[key] = amplified;
        return amplified != null ? amplified : clip;
    }

    public void Preload(AudioClip clip, float gain)
    {
        if (clip == null) return;
        clip.LoadAudioData();
        GetClip(clip, gain);
    }

    public void Dispose()
    {
        foreach (AudioClip amplified in _amplifiedClips.Values)
        {
            if (amplified != null)
            {
                UnityEngine.Object.Destroy(amplified);
            }
        }
        _amplifiedClips.Clear();
    }

    private static AudioClip CreateAmplifiedClip(AudioClip clip, float gain)
    {
        float[] samples = new float[clip.samples * clip.channels];
        if (!clip.GetData(samples, 0))
        {
            Debug.LogWarning($"{clip.name} : 샘플을 읽을 수 없어 Gain을 적용하지 못함. (Load Type이 Decompress On Load여야 함)");
            return null;
        }

        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = Mathf.Clamp(samples[i] * gain, -1f, 1f);
        }

        AudioClip amplified = AudioClip.Create($"{clip.name}_Amplified", clip.samples, clip.channels, clip.frequency, false);
        amplified.SetData(samples, 0);
        return amplified;
    }
}
