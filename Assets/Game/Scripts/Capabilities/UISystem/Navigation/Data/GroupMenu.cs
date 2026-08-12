using CatGame.Core.Enums;
using UnityEngine;

namespace CatGame.Capabilities.UISystem
{
    [System.Serializable]
    public class GroupMenu
    {
        [Header("Menu")]
        [Tooltip("Interfaces que serão ativadas ou desativadas ao trocar entre menus")]
        [SerializeField] private BaseUIScreen panel;

        [Header("Navigation")]
        [Tooltip("Shared: Somente um cursor que qualquer jogador conectado movê-lo. PerPlayer: Cada jogador conectado tem seu próprio cursor.")]
        [SerializeField] private NavigationMode navigationMode = NavigationMode.Shared;

        [Header("Elementos")]
        [Tooltip("Elementos de cada menu")]
        [SerializeField] private GroupSelectable[] groupSelectable;

        public BaseUIScreen Panel => panel;
        public NavigationMode NavigationMode => navigationMode;
        public GroupSelectable[] GroupSelectable => groupSelectable;
    }
}