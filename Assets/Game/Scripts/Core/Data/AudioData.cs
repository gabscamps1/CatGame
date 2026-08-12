using UnityEngine;

namespace CatGame.Core.Data
{
    [CreateAssetMenu(menuName = "Sound/Audio Asset", fileName = "New Audio Asset")]
    public class AudioData : ScriptableObject
    {
        public AudioClip Clip => clip;
        public float Volume => volume;
        public float PitchMin => pitchMin;
        public float PitchMax => pitchMax;

        [SerializeField] private AudioClip clip;
        [SerializeField, Range(0, 1)] private float volume = 1f;
        [SerializeField, Min(0)] private float pitchMin = 1f;
        [SerializeField, Min(0)] private float pitchMax = 1f;
    }
}