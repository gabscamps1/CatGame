using UnityEngine;

namespace CatGame.Capabilities.UISystem
{
    [System.Serializable]
    internal class GroupTab
    {
        [Header("Tabs")]
        [Tooltip("Elementos de cada aba")]
        [SerializeField] private GroupSelectable[] groupSelectable;

        public GroupSelectable[] GroupSelectable => groupSelectable;
    }
}