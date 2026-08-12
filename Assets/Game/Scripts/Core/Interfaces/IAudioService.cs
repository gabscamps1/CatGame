using UnityEngine;

namespace CatGame.Core.Interfaces
{
    public interface IAudioService
    {
        public void PlaySFXClip(Data.AudioData data, bool loop = false);
        public void PlaySFXClip(Data.AudioData data, Vector3? position, bool loop = false);
        public void PlayMusic(Data.AudioData data, bool loop);
        public void StopMusic();
        public bool IsPlayingThisMusic(Data.AudioData data);

        // Controle de Volume.
        public void SetMasterVolume(float value);
        public void SetMusicVolume(float value);
        public void SetSFXVolume(float value);
    }
}

