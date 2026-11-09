using Unity.Netcode;
using UnityEngine;

public class Timer : NetworkBehaviour
{
    public static int CurrentTimer { get; private set; }
    
    [Tooltip("Tempo limite em segundos de duração da partida")]
    [SerializeField] private float gameTimer;

    private float timerCounFloat;
    private readonly NetworkVariable<int> timerCountInt = new();
    private bool isStarted;

    private void Awake()
    {
        GameManager.Instance.OnGameStateChanged += GameManager_OnGameStateChanged;
    }

    private void Update()
    {
        CurrentTimer = timerCountInt.Value;

        if (!IsServer || timerCountInt.Value <= 0) return;
        UpdateTimer();
        UpdateServerTimer();
        CheckTimeOver();
    }

    private void UpdateServerTimer()
    {
        if (!IsServer) return;
        if (timerCountInt.Value == Mathf.RoundToInt(timerCounFloat)) return;
        
        timerCountInt.Value = Mathf.Max((int)timerCounFloat, 0);
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;
        UpdateServerTimer();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer) return;
        timerCounFloat = gameTimer;

        UpdateServerTimer();
    }

    private void GameManager_OnGameStateChanged(GameManager.GameState current)
    {
        if (!IsServer) return;
        switch (current)
        {
            case GameManager.GameState.NotInit:
                ResetTimer();
                break;

            case GameManager.GameState.Playing:
                StartTimer();
                break;

            case GameManager.GameState.GameOver:
                StopTimer();
                break;
        }
    }

    private void ResetTimer()
    {
        if (!IsServer) return;
        isStarted = false;
        timerCounFloat = gameTimer;
    }

    private void StartTimer()
    {
        if (!IsServer) return;
        isStarted = true;
        timerCounFloat = gameTimer;
    }

    private void StopTimer()
    {
        isStarted = false;
    }

    private void UpdateTimer()
    {
        if (!IsServer || !isStarted) return;
        timerCounFloat -= Time.deltaTime;
    }

    private void CheckTimeOver()
    {
        if (timerCountInt.Value > 0 || GameManager.Instance.GetGameState() != GameManager.GameState.Playing) 
            return;

        GameManager.Instance.GameOver();
        ServerMessageManager.Instance.ShowClientMessageRpc(MessageID.TimeOver);
    }
}
