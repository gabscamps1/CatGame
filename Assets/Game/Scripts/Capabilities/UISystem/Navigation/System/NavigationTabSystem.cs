namespace CatGame.Capabilities.UISystem
{
    internal class NavigationTabSystem
    {
        public int CurrentTab { get; private set; }

        private readonly int tabCount;

        public NavigationTabSystem(int tabCount)
        {
            this.tabCount = tabCount;
        }

        public bool TryChangeTab(int tabIndex, out int previousTab)
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
            CurrentTab = 0;
        }
    }
}
