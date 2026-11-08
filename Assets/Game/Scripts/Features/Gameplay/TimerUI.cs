using System;
using TMPro;
using UnityEngine;

public class TimerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;

    private void Update()
    {
        UpdateText();
    }

    private void UpdateText()
    {
        TimeSpan time = TimeSpan.FromSeconds(Timer.CurrentTimer);
        string timerString = time.ToString(@"mm\:ss");
        timerText.text = timerString;
    }
}
