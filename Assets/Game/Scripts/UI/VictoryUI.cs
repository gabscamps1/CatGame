using CatGame.Core.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VictoryUI : MonoBehaviour
{
    [SerializeField] private Image playerImage;
    [SerializeField] private TextMeshProUGUI playerText;
    [SerializeField] private GameObject canvasObject;

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
        canvasObject.SetActive(true);
        playerImage.sprite = e.PlayerSprite;
        playerText.text = e.PlayerId;
    }
}
