namespace CatGame.Core.Data
{
    public class SettingsData
    {
        // Video.
        public int Resolution;
        public int Vsync;
        public int AspectRatio;
        public int WindowMode;

        // Graphics.
        public int Preset;
        public int Texture;
        public int Shadown;
        public int MotionBlur;
        public int Model;

        // Gameplay.
        public string Language;

        // Audio.
        public float MasterVolume;
        public float MusicVolume;
        public float SFXVolume;

        // Controls.
        public string Inputs;
    }
}
