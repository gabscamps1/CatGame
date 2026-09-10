using System.Collections.Generic;

namespace CatGame.Capabilities.UISystem
{
    public class NavigationMenuSystem
    {
        public int CurrentMenu { get; private set; }

        private readonly int menuCount;
        private readonly List<int> menuHistory = new();

        private NavigationTabSystem navigationTabSystem;

        #region Menus      

        public NavigationMenuSystem(int menuCount, int tabCount = 0) 
        {
            this.menuCount = menuCount;
            navigationTabSystem = new NavigationTabSystem(tabCount);
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

        #endregion

        #region Tabs

        public bool TryChangeTab(int tabIndex, out int previousTab)
        {
            return navigationTabSystem.TryChangeTab(tabIndex, out previousTab);
        }

        #endregion
    }
}
