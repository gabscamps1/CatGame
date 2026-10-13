using UnityEngine;

namespace CatGame.UI
{
    public class CharacterSelectionBody : MonoBehaviour
    {
        [Header("Components")]
        //[SerializeField] MMF_Player onSelectFeedback;
        [SerializeField] Animator animator;

        public void OnCharacterSelect()
        {
            /*if (onSelectFeedback != null)
            {
                onSelectFeedback.PlayFeedbacks();
            }*/
        }

        private void Reset()
        {
            /*if (!onSelectFeedback)
                onSelectFeedback = GetComponentInChildren<MMF_Player>();*/

            if (!animator)
                animator = GetComponentInChildren<Animator>();
        }
    } 
}
