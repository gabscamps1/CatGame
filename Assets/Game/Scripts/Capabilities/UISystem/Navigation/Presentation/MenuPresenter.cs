using CatGame.Core;
using CatGame.Core.Enums;
using CatGame.Core.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace CatGame.Capabilities.UISystem
{
    public class MenuPresenter : MonoBehaviour
    {
        [Header("Sub Menu")]
        [SerializeField] private bool isSubMenu;
        [SerializeField] private MenuPresenter parentMenuController;

        [Header("Menus")]
        [SerializeField] private GroupMenu[] menus;    

        private NavigationSystem navigationSystem;
        private NavigationGroup[] navigationGroups;
        private readonly HashSet<PlayerId> activePlayers = new();

        private IUINavigationService navigationService;

        private void Awake()
        {
            navigationSystem = new NavigationSystem(menus.Length);
            navigationGroups = new NavigationGroup[menus.Length];

            for (int i = 0; i < menus.Length; i++)
            {
                NavigationMode navigationMode = menus[i].NavigationMode;
                navigationGroups[i] = new NavigationGroup(navigationMode);
                SetupElements(i);
            }
        }

        private void Start()
        {
            navigationService = ServiceLocator.Get<IUINavigationService>();
            AttachPlayer(PlayerId.P1); // RAFA ME LEMBRA DE REMOVER DEPOIS
            AttachPlayer(PlayerId.P2); // RAFA ME LEMBRA DE REMOVER DEPOIS
        }

        public void AttachPlayer(PlayerId player)
        {
            if (!activePlayers.Add(player)) return;
            navigationService.PushGroup(player, navigationGroups[navigationSystem.CurrentMenu]);
        }

        public void DetachPlayer(PlayerId player)
        {
            if (!activePlayers.Remove(player)) return;
            navigationService.PopGroup(player, navigationGroups[navigationSystem.CurrentMenu]);
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


        #region Functions

        private async void ChangeMenu(int menuIndex)
        {
            if (!navigationSystem.TryChangeMenu(menuIndex, out int previousMenuIndex))
                return;

            foreach (PlayerId player in activePlayers)
                navigationService.PopGroup(player, navigationGroups[previousMenuIndex]);

            await HideGroup(previousMenuIndex);
            await ShowGroup(menuIndex);

            foreach (PlayerId player in activePlayers)
                navigationService.PushGroup(player, navigationGroups[menuIndex]);
        }

        private async void ChangeMenuController(MenuPresenter menuPresenter)
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
            if (isSubMenu && navigationSystem.CurrentMenu == 0)
            {
                // Voltar pro menuPresenter principal.
                ChangeMenuController(parentMenuController);
                return;
            }
            else if (navigationSystem.CurrentMenu == 0)
            {
                CloseMenu();
                return;
            }

            int currentMenu = navigationSystem.CurrentMenu;

            if (!navigationSystem.TryGoBack(out int menuIndex))
                return;

            foreach (PlayerId player in activePlayers)
                navigationService.PopGroup(player, navigationGroups[currentMenu]);

            await HideGroup(currentMenu);
            await ShowGroup(menuIndex);

            foreach (PlayerId player in activePlayers)
                navigationService.PushGroup(player, navigationGroups[menuIndex]);
        }

        private async void CloseMenu()
        {
            int currentMenu = navigationSystem.CurrentMenu;

            foreach (PlayerId player in activePlayers)
                navigationService.PopGroup(player, navigationGroups[currentMenu]);

            activePlayers.Clear();

            // Não seta a interação do menu principal para false ao fechar o menu.
            BaseUIScreen menu = menus[currentMenu].Panel;
            await menu.Hide();

            navigationSystem.Reset();
        }

        #endregion

        #region Config Elements

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
                                button.OnSubmitted += (_, _) => ChangeMenu(numberOfMenu);
                                break;

                            // Chama a função de voltar ao menu anterior.
                            case GroupSelectable.CallFunction.BackToMenu:
                                button.OnSubmitted += (_, _) => ReturnMenu();
                                break;

                            case GroupSelectable.CallFunction.ChangeMenuController:
                                MenuPresenter newMenuController = currentGroupSelectable.NewMenuController;
                                button.OnSubmitted += (_, _) => ChangeMenuController(newMenuController);
                                break;

                            case GroupSelectable.CallFunction.CloseMenu:
                                button.OnSubmitted += (_, _) => CloseMenu();
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
    }
}