using System.Diagnostics;

namespace CatGame.Core
{
    public static class Logger
    {
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Log(string text) => UnityEngine.Debug.Log(text);


        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarning(string text) => UnityEngine.Debug.LogWarning(text);


        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogError(string text) => UnityEngine.Debug.LogError(text);
    }
}
