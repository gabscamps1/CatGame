using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using CatGame.Core.Interfaces;
using CatGame.Core.Data;
using Logger = CatGame.Core.Logger;

namespace CatGame.Services.AudioManagement
{
    public class AudioService : MonoBehaviour, IAudioService
    {
        [Header("Audio Settings")]
        [SerializeField] private AudioMixer audioMixer;

        [Header("SFX Settings")]
        [SerializeField] private int initialSFXObjects;
        [SerializeField] private float minDistance = 1f;
        [SerializeField] private float maxDistance = 20f;
        [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;
        [SerializeField] private AudioSource sfxObject;
        [SerializeField] private AudioMixerGroup sfxMixerGroup;
        private Queue<AudioSource> sfxQueue = new Queue<AudioSource>();

        [Header("Music Settings")]
        [SerializeField] private float fadeOutDuration = 5f;
        [SerializeField] private float musicVolume = 0.6f;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioMixerGroup musicMixerGroup;
        private bool isFadingOut;
        private Coroutine fadeOutCoroutine;
        private Coroutine startCoroutine;

        private void Awake()
        {
            // Inicializa Music Source.
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f; // som 2D
            musicSource.volume = musicVolume;
            if (musicMixerGroup != null)
                musicSource.outputAudioMixerGroup = musicMixerGroup;

            // Inicializa alguns audio sources.
            for (int i = 0; i < initialSFXObjects; i++)
            {
                // Cria um audio source.
                AudioSource audioSource = CreateAudioSource();

                // Coloca o audio source na fila.
                audioSource.gameObject.SetActive(false);
                sfxQueue.Enqueue(audioSource);
            }
        }

        #region Music

        public void PlayMusic(AudioData audio, bool loop)
        {
            if (startCoroutine != null)
                StopCoroutine(startCoroutine);

            startCoroutine = StartCoroutine(StartMusic(audio, loop));
        }

        public void StopMusic()
        {
            // Garante que uma coroutine de música esteja começando enquanto está tentando parar.
            if (startCoroutine != null)
            {
                StopCoroutine(startCoroutine);
                startCoroutine = null;
            }
            
            if (fadeOutCoroutine != null)
                StopCoroutine(fadeOutCoroutine);

            fadeOutCoroutine = StartCoroutine(FadeOutMusic());
        }

        private IEnumerator StartMusic(AudioData audio, bool loop)
        {
            while (isFadingOut)
                yield return null;

            if (IsPlayingMusic())
                yield return FadeOutMusic();

            if (audio != null && audio.Clip != null)
            {
                musicSource.clip = audio.Clip;
                musicSource.volume = audio.Volume;
                musicSource.loop = loop;
                musicSource.Play();
            }
            else
            {
            #if UNITY_EDITOR
                if (audio == null)
                {
                    try
                    {
                        System.Diagnostics.StackTrace stack = new();
                        Logger.LogWarning($"Sem referência de AudioData no script do GameObject: {stack.GetFrame(2).GetMethod().DeclaringType}");
                    }
                    catch
                    {
                        System.Diagnostics.StackTrace stack = new();
                        Logger.LogWarning($"Sem referência de AudioData no script do GameObject: {stack.GetFrame(1).GetMethod().DeclaringType}");
                    }
                }
                else
                {
                    Logger.LogWarning($"Sem referência de AudioClip no SO: {audio.name}.asset");
                }
            #endif
            }

            startCoroutine = null;
        }

        private IEnumerator FadeOutMusic()
        {
            isFadingOut = true;

            if (IsPlayingMusic())
            {
                float startVolume = musicSource.volume;
                float timer = 0;

                while (timer < fadeOutDuration)
                {
                    timer += Time.deltaTime;
                    musicSource.volume = Mathf.Lerp(startVolume, 0, timer / fadeOutDuration);
                    yield return null;
                }

                musicSource.Stop();
                musicSource.volume = musicVolume;
                musicSource.loop = false;
            }

            isFadingOut = false;
            fadeOutCoroutine = null;
        }

        private bool IsPlayingMusic()
        {
            return musicSource.isPlaying;
        }

        public bool IsPlayingThisMusic(AudioData audioData)
        {
            return musicSource.isPlaying && musicSource.clip == audioData.Clip;
        }

        #endregion

        #region SFX

        /// <summary>
        /// Cria um audio source configurado.
        /// </summary>
        /// <returns></returns>
        private AudioSource CreateAudioSource()
        {
            // Instancia um novo audio source.
                AudioSource audioSource = Instantiate(sfxObject, transform.position, Quaternion.identity, transform.parent);

            // Configura o audio source.
            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;
            audioSource.rolloffMode = rolloffMode;
            audioSource.outputAudioMixerGroup = sfxMixerGroup;
            audioSource.loop = false;

            return audioSource;
        }

        /// <summary>
        /// Pega um audio source da fila.
        /// </summary>
        /// <returns></returns>
        private AudioSource GetAudioSource()
        {
            if (sfxQueue.Count > 0)
            {
                AudioSource audioSource = sfxQueue.Dequeue();
                audioSource.gameObject.SetActive(true);
                return audioSource;
            }
            else
            {
                return CreateAudioSource();
            }
        }

        public void PlaySFXClip(AudioData audio, bool loop = false)
        {
            PlaySFXClip(audio, transform.position, loop);
        }

        public void PlaySFXClip(AudioData audio, Vector3? position, bool loop = false)
        {
            if (audio != null && audio.Clip != null)
            {
                AudioSource currentSFX = GetAudioSource();

                currentSFX.clip = audio.Clip;
                currentSFX.volume = audio.Volume;
                currentSFX.spatialBlend = position.HasValue ? 1 : 0;
                currentSFX.transform.position = position ?? new();
                currentSFX.pitch =  Random.Range(audio.PitchMin, audio.PitchMax);
                currentSFX.loop = loop;
                currentSFX.Play();

                float delay = audio.Clip.length;
                StartCoroutine(EnqueueSFX(currentSFX, delay));
            }
            else
            {
                #if UNITY_EDITOR
                    if (audio == null)
                    {
                        try
                        {
                            System.Diagnostics.StackTrace stack = new();
                            Logger.LogWarning($"Sem referência de AudioData no script do GameObject: {stack.GetFrame(2).GetMethod().DeclaringType}");
                        }
                        catch
                        {
                            System.Diagnostics.StackTrace stack = new();
                            Logger.LogWarning($"Sem referência de AudioData no script do GameObject: {stack.GetFrame(1).GetMethod().DeclaringType}");
                        }
                    }
                    else
                    {
                        Logger.LogWarning($"Sem referência de AudioClip no SO: {audio.name}.asset");
                    }
                #endif
            }
        }

        private IEnumerator EnqueueSFX(AudioSource audioSource, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);

            audioSource.gameObject.SetActive(false);
            sfxQueue.Enqueue(audioSource);
        }

        #endregion

        #region Controle de Volume

        public void SetMasterVolume(float value)
        {
            audioMixer.SetFloat("MasterVol", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f);
        }

        public void SetMusicVolume(float value)
        {
            audioMixer.SetFloat("MusicVol", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f);
        }

        public void SetSFXVolume(float value)
        {
            audioMixer.SetFloat("SFXVol", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f);
        }

        #endregion
    }
}