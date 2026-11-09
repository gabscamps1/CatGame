using CatGame.Core;
using CatGame.Core.Events;
using CatGame.Core.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VictoryUI : MonoBehaviour
{
    [SerializeField] private Image playerImage;
    [SerializeField] private RawImage stripesImage;
    [SerializeField] private Image playerBG;
    [SerializeField] private TextMeshProUGUI playerText;
    [SerializeField] private GameObject victoryRoot;
    [SerializeField] private string victoryID;

    [SerializeField] private Color colorP1 = new Color(255f, 0f, 0f);
    [SerializeField] private Color colorP2 = new Color(255f, 0f, 0f);
    [SerializeField] private Color colorP3 = new Color(255f, 0f, 0f);
    [SerializeField] private Color colorP4 = new Color(255f, 0f, 0f);

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
        int playerId = e.PlayerId.Id;

        Debug.Log(playerId);

        string player = playerId switch
        {
            0 => "P1",
            1 => "P2",
            2 => "P3",
            3 => "P4",
            _ => ""
        };

        // Obtém a cor correspondente ao ID do jogador
        Color playerColor = playerId switch
        {
            0 => colorP1,
            1 => colorP2,
            2 => colorP3,
            3 => colorP4,
            _ => Color.white
        };

        string victoryText = ServiceLocator
            .Get<ILocalizationService>()
            .GetTextByID(victoryID);

        // Atualiza os elementos da interface
        playerImage.sprite = e.PlayerSprite;

        playerText.text = $"{player} {victoryText}";
        playerText.color = playerColor;

        playerBG.color = playerColor;
        stripesImage.color = playerColor;

        victoryRoot.SetActive(true);
    }
}