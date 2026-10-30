
using CatGame.Core.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace CatGame.UI
{
    public class MainUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image nextFruitImage;
        [SerializeField] private TextMeshProUGUI score;
        [SerializeField] private TextMeshProUGUI ping;
        [SerializeField] private GameObject crowObject;

        [Header("Score Pop Effect")]
        [SerializeField] private Color baseTextColor = Color.white;

        [SerializeField, Min(0.01f)]
        private float popDuration = 0.3f;

        [SerializeField, Min(1f)]
        private float popScale = 1.3f;

        [Header("Permission")]
        [SerializeField] private int playerIndex;

        private Coroutine popCoroutine;

        private Vector3 baseScale;

        private void Awake()
        {
            EventBus.Subscribe<LeaderChangedEvent>(EventBus_ActiveCrow);
            EventBus.Subscribe<NextFruitChangedEvent>(EventBus_UpdateNextFruit);
            EventBus.Subscribe<ScoreChangedEvent>(EventBus_UpdateScore);
            EventBus.Subscribe<ClientPingChangedEvent>(EventBus_UpdatePing);

            baseScale = score.rectTransform.localScale;

            score.text = "0";
            score.color = baseTextColor;

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

            if (popCoroutine != null)
                StopCoroutine(popCoroutine);

            popCoroutine = StartCoroutine(ScorePopEffect());
        }

        private IEnumerator ScorePopEffect()
        {
            float elapsed = 0f;

            while (elapsed < popDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(elapsed / popDuration);

                // Intensidade do efeito: 0 -> 1 -> 0
                float effect = Mathf.Sin(t * Mathf.PI);

                // Efeito de cor
                score.color = Color.Lerp(
                    baseTextColor,
                    Color.white,
                    effect
                );

                // Efeito de escala
                float currentScale = Mathf.Lerp(
                    1f,
                    popScale,
                    effect
                );

                score.rectTransform.localScale = baseScale * currentScale;

                yield return null;
            }

            // Restaura os valores originais
            score.color = baseTextColor;
            score.rectTransform.localScale = baseScale;

            popCoroutine = null;
        }

        private void EventBus_UpdatePing(ClientPingChangedEvent e)
        {
            if (e.PlayerIndex != playerIndex)
                return;

            ping.text = e.Ping.ToString();
        }
    }
}
