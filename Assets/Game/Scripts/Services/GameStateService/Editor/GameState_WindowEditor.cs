using CatGame.Core.Enums;
using CatGame.Core;
using UnityEditor;
using UnityEngine;
using CatGame.Core.Interfaces;

namespace Assets.Scripts.Services
{
    public class GameState_WindowEditor : EditorWindow
    {
        [MenuItem("Tools/QBProj/UIManager")]
        static void Init()
        {
            GetWindow<GameState_WindowEditor>().Show();          
        }

        public void OnGUI()
        {
            if (GUILayout.Button("Main Menu"))
                ChangeState(GameState.MainMenu);

            if (GUILayout.Button("Loading"))
                ChangeState(GameState.Loading);

            if (GUILayout.Button("Pause"))
                ChangeState(GameState.Paused);

            if (GUILayout.Button("Playing"))
                ChangeState(GameState.Playing);
        }

        private void ChangeState(GameState state)
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Game States", "Você precisa estar em PlayMode", "OK");
                return;
            }

            IGameStateService gameStateService = ServiceLocator.Get<IGameStateService>();

            if (gameStateService == null)
                return;

            gameStateService.SetState(state);
        }
    }
}