using CatGame.Core.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CatGame.UI
{
    public class MainUI : MonoBehaviour
    {
        [Header("UI Refenreces")]
        [SerializeField] private Image nextFruitImage;
        [SerializeField] private TextMeshProUGUI score;
        [SerializeField] private TextMeshProUGUI ping;
        [SerializeField] private GameObject crowObject;

        [Header("Permission")]
        [SerializeField] private int playerIndex; // A quem pertence essa interface. (ex: 0 = player 1, 1 = player 2, ...)

        private void Awake()
        {
            EventBus.Subscribe<LeaderChangedEvent>(EventBus_ActiveCrow);
            EventBus.Subscribe<NextFruitChangedEvent>(EventBus_UpdateNextFruit);
            EventBus.Subscribe<ScoreChangedEvent>(EventBus_UpdateScore);
            EventBus.Subscribe<ClientPingChangedEvent>(EventBus_UpdatePing);

            score.text = "0";
            ping.text = "";
            crowObject.SetActive(false);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<LeaderChangedEvent>(EventBus_ActiveCrow);
            EventBus.Unsubscribe<NextFruitChangedEvent>(EventBus_UpdateNextFruit);
            EventBus.Unsubscribe<ScoreChangedEvent>(EventBus_UpdateScore);
            EventBus.Unsubscribe<ClientPingChangedEvent>(EventBus_UpdatePing);
        }

        private void EventBus_ActiveCrow(LeaderChangedEvent e)
        {
            if (e.PlayerIndex != playerIndex)
                return;

            crowObject.SetActive(e.IsLeader);
        }

        private void EventBus_UpdateNextFruit(NextFruitChangedEvent e)
        {
            if (e.PlayerIndex != playerIndex)
                return;

            nextFruitImage.sprite = e.Sprite;
        }

        private void EventBus_UpdateScore(ScoreChangedEvent e)
        {
            if (e.PlayerIndex != playerIndex)
                return;

            score.text = e.ScoreAmount.ToString();
        }

        private void EventBus_UpdatePing(ClientPingChangedEvent e)
        {
            if (e.PlayerIndex != playerIndex)
                return;

            ping.text = e.Ping.ToString();
        }
    }
}