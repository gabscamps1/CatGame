using System.Collections.Generic;

namespace CatGame.Capabilities.UISystem
{
    public class NavigationMenuSystem
    {
        public int CurrentMenu { get; private set; }

        private readonly int menuCount;
        private readonly List<int> menuHistory = new();

        public NavigationMenuSystem(int menuCount) 
        {
            this.menuCount = menuCount;
        }

        public bool TryChangeMenu(int menuIndex, out int previousMenu)
        {
            previousMenu = CurrentMenu;
            if (menuIndex < 0 || menuIndex >= menuCount) return false;

            menuHistory.Add(CurrentMenu);
            CurrentMenu = menuIndex;
            return true;
        }

        /// <summary>
        /// Vai direto para um menu e troca o histórico inteiro, como se o jogador tivesse passado pelos menus do histórico.
        /// </summary>
        public bool TrySetMenu(int menuIndex, IReadOnlyList<int> history, out int previousMenu)
        {
            previousMenu = CurrentMenu;
            if (!IsValidMenu(menuIndex)) return false;

            foreach (int historyMenu in history)
                if (!IsValidMenu(historyMenu)) return false;

            menuHistory.Clear();
            menuHistory.AddRange(history);
            CurrentMenu = menuIndex;
            return true;
        }

        public bool TryGoBack(out int menuIndex)
        {
            if (menuHistory.Count == 0)
            {
                menuIndex = 0;
                return false;
            }

            menuIndex = menuHistory[^1];
            CurrentMenu = menuIndex;
            menuHistory.RemoveAt(menuHistory.Count - 1);

            return true;
        }

        public void Reset()
        {
            menuHistory.Clear();
            CurrentMenu = 0;
        }

        private bool IsValidMenu(int menuIndex) => menuIndex >= 0 && menuIndex < menuCount;
    }
}
