using UnityEngine;

namespace CatGame.Capabilities.UISystem
{
    [System.Serializable]
    public class GroupTab
    {
        [Header("Tabs")]
      
        [Tooltip("GameObject Pai da Aba")]
        [SerializeField] private UIScreen tabScreen;
        
        [Tooltip("Elementos de cada aba")]
        [SerializeField] private GroupSelectable[] groupSelectable;

        public UIScreen TabScreen => tabScreen;
        public GroupSelectable[] GroupSelectable => groupSelectable;
    }
}