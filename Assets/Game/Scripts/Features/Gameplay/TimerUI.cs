using CatGame.Core;
using CatGame.Core.Interfaces;
using CatGame.Core.Data;
using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class TimerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Color textColor;
    [SerializeField] private float timePulseDefeatText;

    private IAudioService audioService;
    private bool is20SecondsRemaining;
    private bool isRunningLastCoroutine;
    private int currentTime;

    private Vector3 initialLocalSize;
    private Vector3 toLocalSize;

    [Header("Sounds")]
    [SerializeField] private AudioData UI_REMAINING;
    [SerializeField] private AudioData UI_BEEP;


    private void Awake()
    {
        initialLocalSize = timerText.transform.localScale;
        toLocalSize = timerText.transform.localScale * 1.3f;
    }

    private void Start()
    {
        audioService = ServiceLocator.Get<IAudioService>();
    }

    private void Update()
    {
        UpdateText();
        CheckTimeRemaining();
    }
 
    private void UpdateText()
    {
        TimeSpan time = TimeSpan.FromSeconds(Timer.CurrentTimer);
        string timerString = time.ToString(@"mm\:ss");
        timerText.text = timerString;  
    }

    private void CheckTimeRemaining()
    {
        if (GameManager.Instance.GetGameState() != GameManager.GameState.Playing)
            return;

        if (Timer.CurrentTimer <= 20)
        {
            if (!is20SecondsRemaining)
            {
                is20SecondsRemaining = true;
                timerText.color = Color.red;
                audioService.PlaySFXClip(UI_REMAINING, false);
            }
        }

        if (Timer.CurrentTimer <= 5)
        {
            if (!isRunningLastCoroutine)
                StartCoroutine(StartLastSeconds());
        } 
    }

    private IEnumerator StartLastSeconds()
    {
        isRunningLastCoroutine = true;

        while (Timer.CurrentTimer > 0)
        {
            yield return new WaitUntil(() => currentTime != Timer.CurrentTimer);
            currentTime = Timer.CurrentTimer;

            audioService.PlaySFXClip(UI_BEEP, false);

            // Efeito de pulso.
            LeanTween.scale(timerText.gameObject, toLocalSize, timePulseDefeatText).setOnComplete(
                () =>
                {
                    LeanTween.scale(timerText.gameObject, initialLocalSize, timePulseDefeatText);
                });

            yield return null;
        }
    }
}
