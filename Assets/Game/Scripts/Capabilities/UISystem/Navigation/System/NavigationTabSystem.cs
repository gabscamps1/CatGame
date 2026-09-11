using CatGame.Core;

namespace CatGame.Capabilities.UISystem
{
    public class NavigationTabSystem
    {
        public int CurrentTab { get; private set; }

        private readonly int tabCount;

        public NavigationTabSystem(int tabCount)
        {
            this.tabCount = tabCount;
        }

        public bool TryNextTab(out int previousTab)
        {
            int goingTab = CurrentTab + 1 % tabCount;
            return TryChangeTab(goingTab, out previousTab);
        }

        public bool TryPreviousTab(out int previousTab)
        {
            int goingTab = CurrentTab - 1 == 0 
                ? tabCount - 1 
                : CurrentTab - 1;

            return TryChangeTab(goingTab, out previousTab);
        }

        private bool TryChangeTab(int tabIndex, out int previousTab)
        {       
            previousTab = CurrentTab;

            if (tabCount == 0)
                return false;

            if (tabIndex < 0 || tabIndex >= tabCount) return false;

            CurrentTab = tabIndex;
            return true;
        }

        public void Reset()
        {
            if (!TryChangeTab(0, out int previousTab))
                Logger.LogError("Não foi possível de retornar a aba");
        }
    }
}
