using CatGame.Core.Events;
using System.Collections;
using UnityEngine;

public class DefeatStripesController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int playerId;
    [SerializeField] private FadeInLoseTape[] stripes;
    [SerializeField] private GameObject defeatTextObject;
    [SerializeField] private float timePulseDefeatText;

    private void Awake()
    {
        EventBus.Subscribe<PlayerDefeatedEvent>(OnPlayerDefeated);

        defeatTextObject.SetActive(false);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<PlayerDefeatedEvent>(OnPlayerDefeated);
    }

    private void OnPlayerDefeated(PlayerDefeatedEvent e)
    {
        if (playerId != e.PlayerId.Id)
            return;

        ExecuteDefeatAnimation();
    }

    private void ExecuteDefeatAnimation()
    {
        StartCoroutine(DefeatAnimation());
    }

    private IEnumerator DefeatAnimation()
    {
        for (int i = 0; i < stripes.Length; i++)
        {
            stripes[i].PlayEffect();
            yield return new WaitUntil(() => stripes[i].IsFinished);
        }

        defeatTextObject.SetActive(true);

        Vector3 initial = defeatTextObject.transform.localScale;
        Vector3 to = defeatTextObject.transform.localScale * 1.3f;

        // Efeito de pulso.
        LeanTween.scale(defeatTextObject, to, timePulseDefeatText).setOnComplete(
            () =>
            {
                LeanTween.scale(defeatTextObject, initial, timePulseDefeatText);
            });
    }
}
