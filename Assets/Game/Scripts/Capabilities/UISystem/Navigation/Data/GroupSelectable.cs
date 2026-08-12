using UnityEngine;

namespace CatGame.Capabilities.UISystem
{
    [System.Serializable]
    public class GroupSelectable
    {      
        [Tooltip("Funções que podem ser chamadas pelo botão.")]
        public enum CallFunction
        {
            [Tooltip("Não realiza nenhuma ação.")]
            None, // Não faz nada.

            [Tooltip("Acessa outro menu desse MenuController.")]
            ChangeMenu, // Troca para outro menu que está na lista GroupMenu.

            [Tooltip("Volta para o menu anterior.")]
            BackToMenu, // Volta para o menu anterior.

            [Tooltip("Acessa o menu de outro MenuController.")]
            ChangeMenuController, // Troca para um menu de outro script MenuController.         

            [Tooltip("Fecha esse MenuController.")]
            CloseMenu,
        }
       
        [Tooltip("Elementos de cada menu")]
        [SerializeField] private NavigableElement element;

        [Tooltip("Função que será chamada ao pressionar o elemento. Obs: Somente se o elemento for um botão")]
        [SerializeField] private CallFunction function;

        [Tooltip("Menu que será carregado pela função ChangeMenu")]
        [SerializeField] private int numberOfMenu;

        [Tooltip("MenuController que será carregado pela função ChangeMenuController")]
        [SerializeField] private MenuPresenter newMenuController;

        public NavigableElement Element => element;
        public CallFunction Function => function;
        public int NumberOfMenu => numberOfMenu;
        public MenuPresenter NewMenuController => newMenuController;
    }
}