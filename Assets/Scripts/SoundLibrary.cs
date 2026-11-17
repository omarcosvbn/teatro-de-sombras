using System.Collections.Generic;
using UnityEngine;

public enum SoundCategory {
    Sfx,
    Music,
    Voice
}

// Every sound the Sound window can offer, by category. Fill it in the Inspector.
[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Teatro de Sombras/Sound Library")]
public sealed class SoundLibrary : ScriptableObject {
    [SerializeField] private AudioClip[] sfxClips = new AudioClip[0];
    [SerializeField] private AudioClip[] musicClips = new AudioClip[0];
    [SerializeField] private AudioClip[] voiceClips = new AudioClip[0];

    public IReadOnlyList<AudioClip> GetClips(SoundCategory category) {
        switch (category) {
            case SoundCategory.Music:
                return musicClips;
            case SoundCategory.Voice:
                return voiceClips;
            default:
                return sfxClips;
        }
    }

    public static string DisplayName(SoundCategory category) {
        switch (category) {
            case SoundCategory.Music:
                return "Music";
            case SoundCategory.Voice:
                return "Voice";
            default:
                return "SFX";
        }
    }
}
