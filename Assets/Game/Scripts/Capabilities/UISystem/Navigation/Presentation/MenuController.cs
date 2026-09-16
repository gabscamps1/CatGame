using CatGame.Core;
using CatGame.Core.Enums;
using CatGame.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace CatGame.Capabilities.UISystem
{
    public class MenuController : MonoBehaviour
    {
        public IReadOnlyList<PlayerId> ActivePlayers => activePlayers.ToList();
        public int CurrentMenuIndex => navigationMenuSystem.CurrentMenu;
        public GroupMenu CurrentGroupMenu => menus[CurrentMenuIndex];
        public NavigationTabSystem CurrentNavigationTabSystem => navigationTabSystem[CurrentMenuIndex];
        public int CurrentTabIndex => CurrentNavigationTabSystem.CurrentTab;
        public NavigationGroup CurrentNavigationGroup => navigationGroups[CurrentMenuIndex];


        [Header("Menu Settings")]
        [SerializeField] private bool isCloseableMenu;

        [Header("Sub Menu Settings")]
        [SerializeField] private bool isSubMenu;
        [SerializeField] private MenuController parentMenuController;

        [Header("Menus")]
        [SerializeField] private GroupMenu[] menus;    

        private NavigationGroup[] navigationGroups;
        private NavigationTabSystem[] navigationTabSystem;
        private NavigationMenuSystem navigationMenuSystem;

        private readonly HashSet<PlayerId> activePlayers = new();

        private IUINavigationService navigationService;
        private IInputService inputService;

        private void Awake()
        {
            navigationMenuSystem = new NavigationMenuSystem(menus.Length);
            navigationTabSystem = new NavigationTabSystem[menus.Length];
            navigationGroups = new NavigationGroup[menus.Length];

            for (int i = 0; i < menus.Length; i++)
            {
                NavigationMode navigationMode = menus[i].NavigationMode;
                navigationGroups[i] = new NavigationGroup(navigationMode);
                navigationGroups[i].OnCancelRequested += NavigationGroup_OnCancelRequested;

                int tabCount = menus[i].GroupTab.Length;
                navigationTabSystem[i] = new NavigationTabSystem(tabCount);

                SetupElements(i);
            }
        }

        private void Start()
        {
            navigationService = ServiceLocator.Get<IUINavigationService>();
            inputService = ServiceLocator.Get<IInputService>();

            AttachPlayer(PlayerId.P1); // REMOVER DEPOIS - ME LEMBRA PFV SALLES
        }

        private void ConfigInputs(params PlayerId[] playersToRemove)
        {
            foreach (PlayerId playerToRemove in playersToRemove)
            {
                inputService.GetInputFromPlayer(playerToRemove).OnTabNavigation -= MenuController_OnTabNavigation;
            }

            foreach (PlayerId player in activePlayers)
            {
                inputService.GetInputFromPlayer(player).OnTabNavigation -= MenuController_OnTabNavigation;
                inputService.GetInputFromPlayer(player).OnTabNavigation += MenuController_OnTabNavigation;
            }
        }

        private void MenuController_OnTabNavigation(int tabIncreasement)
        {
            ChangeTab(tabIncreasement);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < menus.Length; i++) 
                navigationGroups[i].OnCancelRequested -= NavigationGroup_OnCancelRequested;

            foreach (PlayerId player in activePlayers)
                navigationService.ForceRemoveGroup(player, navigationGroups[navigationMenuSystem.CurrentMenu]);
        }   
        

        public void AttachPlayer(PlayerId player)
        {
            if (!activePlayers.Add(player)) return;
            navigationService.PushGroup(player, navigationGroups[navigationMenuSystem.CurrentMenu]);

            ConfigInputs();
        }

        public void DetachPlayer(PlayerId player)
        {
            if (!activePlayers.Remove(player)) return;
            navigationService.PopGroup(player, navigationGroups[navigationMenuSystem.CurrentMenu]);

            ConfigInputs(player);
        }

        public void DetachAllPlayers()
        {
            foreach (PlayerId player in activePlayers)
            {
                if (!activePlayers.Remove(player)) continue;
                navigationService.PopGroup(player, navigationGroups[navigationMenuSystem.CurrentMenu]);
            }
        }

        private async Task ShowGroup(int index)
        {
            BaseUIScreen menu = menus[index].Panel;
            await menu.Show();

            SetInteractionElements(index, true);
        }

        private async Task HideGroup(int index)
        {
            SetInteractionElements(index, false);

            BaseUIScreen menu = menus[index].Panel;
            await menu.Hide();
        }

        private async Task ShowTab(int tabIndex)
        {
            if (menus[CurrentMenuIndex].GroupTab == null || menus[CurrentMenuIndex].GroupTab.Length == 0)
                return;

            UIScreen tab = menus[CurrentMenuIndex].GroupTab[tabIndex].TabScreen;

            if (tab != null)
                await tab.Show();

            SetInteractionElements(CurrentMenuIndex, true);
        }

        private async Task HideTab(int menuIndex, int tabIndex)
        {
            if (menus[menuIndex].GroupTab == null || menus[menuIndex].GroupTab.Length == 0) 
                return;

            SetInteractionElements(menuIndex, false);

            UIScreen tab = menus[menuIndex].GroupTab[tabIndex].TabScreen;

            if (tab != null)
                await tab.Hide();
        }

        private async void ChangeTab(int tabIncreasement)
        {
            int previousTab = default;

            bool isTabChanged = tabIncreasement switch
            {
                -1 => CurrentNavigationTabSystem.TryPreviousTab(out previousTab),
                1 => CurrentNavigationTabSystem.TryNextTab(out previousTab),
                _ => false
            };

            if (!isTabChanged)
                return;

            await HideTab(CurrentMenuIndex, previousTab);
            await ShowTab(CurrentTabIndex);
            CurrentNavigationGroup.ResetElement();
        }

        #region Functions

        private async void ChangeMenu(int menuIndex)
        {
            if (!navigationMenuSystem.TryChangeMenu(menuIndex, out int previousMenuIndex))
                return;

            bool shouldReset = menus[previousMenuIndex].ResetOnForwardTransition;

            foreach (PlayerId player in activePlayers)
            {
                if (shouldReset)
                    navigationGroups[previousMenuIndex].ResetFocusToDefault(player);

                navigationService.PopGroup(player, navigationGroups[previousMenuIndex]);
            }

            await HideTab(previousMenuIndex, navigationTabSystem[previousMenuIndex].CurrentTab);

            await HideGroup(previousMenuIndex);
            await ShowGroup(menuIndex);
            await ShowTab(CurrentTabIndex);

            foreach (PlayerId player in activePlayers)
                navigationService.PushGroup(player, navigationGroups[menuIndex]);
        }

        private async void ChangeMenuController(MenuController menuPresenter)
        {
            /*if (menuPresenter == null)
                return;

            if (menuPresenter.navigationSystem == null)
                return;

            foreach (PlayerId player in activePlayers)
                navigationService.PopGroup(player, navigationGroups[navigationSystem.CurrentMenu]);

            await HideGroup(navigationSystem.CurrentMenu);
            enabled = false;

            await menuPresenter.ShowGroup(menuPresenter.navigationSystem.CurrentMenu);
            menuPresenter.enabled = true;

            foreach (PlayerId player in activePlayers)
                navigationService.PushGroup(player, menuPresenter.navigationGroups[menuPresenter.navigationSystem.CurrentMenu]);*/
        }

        private async void ReturnMenu()
        {
            if (isSubMenu && navigationMenuSystem.CurrentMenu == 0)
            {
                // Voltar pro menuPresenter principal.
                ChangeMenuController(parentMenuController);
                return;
            }
            else if (isCloseableMenu && navigationMenuSystem.CurrentMenu == 0)
            {
                CloseMenu();
                return;
            }

            int currentMenu = navigationMenuSystem.CurrentMenu;
            bool shouldReset = menus[currentMenu].ResetOnBackwardTransition;

            if (!navigationMenuSystem.TryGoBack(out int newMenu))
                return;

            foreach (PlayerId player in activePlayers)
            {
                if (shouldReset)
                    navigationGroups[currentMenu].ResetFocusToDefault(player);

                navigationService.PopGroup(player, navigationGroups[currentMenu]);
            }

            await HideTab(currentMenu, navigationTabSystem[currentMenu].CurrentTab);
            navigationTabSystem[currentMenu].Reset();

            await HideGroup(currentMenu);
            await ShowGroup(newMenu);
            await ShowTab(newMenu);

            foreach (PlayerId player in activePlayers)
                navigationService.PushGroup(player, navigationGroups[newMenu]);
        }

        private async void CloseMenu()
        {
            int currentMenu = navigationMenuSystem.CurrentMenu;
            bool shouldReset = menus[currentMenu].ResetOnBackwardTransition;

            foreach (PlayerId player in activePlayers)
            {
                if (shouldReset)
                    navigationGroups[currentMenu].ResetFocusToDefault(player);

                navigationService.PopGroup(player, navigationGroups[currentMenu]);
            }

            activePlayers.Clear();

            await HideTab(currentMenu, navigationTabSystem[currentMenu].CurrentTab);
            navigationTabSystem[currentMenu].Reset();

            // Não seta a interação do menu principal para false ao fechar o menu.
            BaseUIScreen menu = menus[currentMenu].Panel;
            await menu.Hide();

            navigationMenuSystem.Reset();
        }

        #endregion

        #region Config Elements

        // CORRIGIR AS CHAMADAS 
        private void SetupElements(int menu)
        {
            GroupMenu currentMenu = menus[menu];
            NavigationGroup group = navigationGroups[menu];

            for (int i = 0; i < currentMenu.GroupTab.Length; i++)
            {
                GroupTab currentTab = currentMenu.GroupTab[i];

                for (int j = 0; j < currentTab.GroupSelectable.Length; j++)
                {
                    GroupSelectable currentGroupSelectable = currentTab.GroupSelectable[j];
                    NavigableElement element = currentGroupSelectable.Element;

                    if (element == null)
                        continue;

                    group.Register(element);

                    switch (element)
                    {
                        case ButtonElement button:

                            switch (currentGroupSelectable.Function)
                            {
                                // Chama a funlçao de trocar menu.
                                case GroupSelectable.CallFunction.ChangeMenu:
                                    int numberOfMenu = currentGroupSelectable.NumberOfMenu;
                                    button.OnSubmittedEvent += (_, _) => ChangeMenu(numberOfMenu);
                                    break;

                                // Chama a função de voltar ao menu anterior.
                                case GroupSelectable.CallFunction.BackToMenu:
                                    button.OnSubmittedEvent += (_, _) => ReturnMenu();
                                    break;

                                case GroupSelectable.CallFunction.ChangeMenuController:
                                    MenuController newMenuController = currentGroupSelectable.NewMenuController;
                                    button.OnSubmittedEvent += (_, _) => ChangeMenuController(newMenuController);
                                    break;

                                case GroupSelectable.CallFunction.CloseMenu:
                                    button.OnSubmittedEvent += (_, _) => CloseMenu();
                                    break;
                            }

                            break;

                        default:
                            Core.Logger.LogWarning("[MenuPresenter] Classe que herda de NavigableElement não foi registrada no menu.");
                            break;
                    }
                }      
            }
        }

        private void SetInteractionElements(int menu, bool isToActive)
        {
            GroupMenu currentMenu = menus[menu];

            foreach (var tab in currentMenu.GroupTab)
                foreach (var groupSelectable in tab.GroupSelectable)
                    groupSelectable.Element?.SetInteractable(isToActive);
        }

        #endregion            

        private void NavigationGroup_OnCancelRequested(object sender, PlayerId e)
        {
            ReturnMenu();
        }
    }
}