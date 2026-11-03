using System;
using TMPro;
using UnityEngine;

public class TimerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Timer timer;

    private void Update()
    {
        UpdateText();
    }

    private void UpdateText()
    {
        TimeSpan time = TimeSpan.FromSeconds(timer.CurrentTimer);
        string timerString = time.ToString(@"mm\:ss");
        timerText.text = timerString;
    }
}
