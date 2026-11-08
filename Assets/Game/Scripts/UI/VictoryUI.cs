using CatGame.Core;
using CatGame.Core.Events;
using CatGame.Core.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VictoryUI : MonoBehaviour
{
    [SerializeField] private Image playerImage;
    [SerializeField] private TextMeshProUGUI playerText;
    [SerializeField] private GameObject victoryRoot;
    [SerializeField] private string victoryID;

    private void Awake()
    {
        EventBus.Subscribe<PlayerWonEvent>(UpdateUI); 
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<PlayerWonEvent>(UpdateUI); 
    }

    private void UpdateUI(PlayerWonEvent e)
    {
        Debug.Log(e.PlayerId.Id);
        string player = e.PlayerId.Id switch
        {
            0 => "P1",
            1 => "P2",
            2 => "P3",
            3 => "P4",
            _ => ""
        };

        string victoryText = ServiceLocator.Get<ILocalizationService>().GetTextByID(victoryID);

        playerImage.sprite = e.PlayerSprite;
        playerText.text = $"{player} {victoryText}";

        victoryRoot.SetActive(true);
    }
}
