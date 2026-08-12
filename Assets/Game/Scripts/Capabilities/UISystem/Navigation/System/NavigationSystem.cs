using System.Collections.Generic;

namespace CatGame.Capabilities.UISystem
{
    public class NavigationSystem
    {
        public int CurrentMenu { get; private set; }

        private readonly int menuCount;
        private readonly List<int> menuHistory = new();

        public NavigationSystem(int menuCount) 
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
    }
}
