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
        public NavigationGroup CurrentNavigationGroup => navigationGroups[CurrentMenu];
        public NavigationTabSystem CurrentNavigationTabSystem => navigationTabSystems[CurrentMenu];
        public int CurrentTab => navigationTabSystems[CurrentMenu].CurrentTab;
        public int CurrentMenu => navigationMenuSystem.CurrentMenu;

        [Header("Menu Settings")]
        [SerializeField] private bool isCloseableMenu;

        [Header("Sub Menu Settings")]
        [SerializeField] private bool isSubMenu;
        [SerializeField] private MenuController parentMenuController;

        [Header("Menus")]
        [SerializeField] private GroupMenu[] menus;    

        private NavigationGroup[] navigationGroups;
        private NavigationTabSystem[] navigationTabSystems;
        private NavigationMenuSystem navigationMenuSystem;

        private readonly HashSet<PlayerId> activePlayers = new();

        private IUINavigationService navigationService;
        private IInputService inputService;

        private void Awake()
        {
            navigationMenuSystem = new NavigationMenuSystem(menus.Length);
            navigationTabSystems = new NavigationTabSystem[menus.Length];
            navigationGroups = new NavigationGroup[menus.Length];

            for (int i = 0; i < menus.Length; i++)
            {
                NavigationMode navigationMode = menus[i].NavigationMode;
                navigationGroups[i] = new NavigationGroup(navigationMode);
                navigationGroups[i].OnCancelRequested += NavigationGroup_OnCancelRequested;

                int tabCount = menus[i].GroupTab.Length;
                navigationTabSystems[i] = new NavigationTabSystem(tabCount);

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
            Debug.Log(tabIncreasement.ToString());
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
            Debug.Log("Testando" + player);
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
            BaseUIScreen tab = menus[CurrentMenu].GroupTab[tabIndex].TabScreen;
            await tab.Show();

            SetInteractionElements(CurrentMenu, true);
        }

        private async Task HideTab(int tabIndex)
        {
            SetInteractionElements(CurrentMenu, false);

            BaseUIScreen tab = menus[CurrentMenu].GroupTab[tabIndex].TabScreen;
            await tab.Show();
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
            Debug.Log(CurrentTab.ToString());
            await HideTab(previousTab);
            await ShowTab(CurrentTab);
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

            await HideGroup(previousMenuIndex);
            await ShowGroup(menuIndex);

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

            await HideGroup(currentMenu);
            await ShowGroup(newMenu);

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

            for (int i = 0; i < currentMenu.GroupSelectable.Length; i++)
            {
                GroupSelectable currentGroupSelectable = currentMenu.GroupSelectable[i];
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

        private void SetInteractionElements(int menu, bool isToActive)
        {
            GroupMenu currentMenu = menus[menu];

            foreach (var group in currentMenu.GroupSelectable)
                group.Element?.SetInteractable(isToActive);
        }

        #endregion            

        private void NavigationGroup_OnCancelRequested(object sender, PlayerId e)
        {
            ReturnMenu();
        }
    }
}