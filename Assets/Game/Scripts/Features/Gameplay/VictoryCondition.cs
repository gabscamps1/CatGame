using CatGame.Core;
using CatGame.Core.Data;
using CatGame.Core.Enums;
using CatGame.Core.Events;
using CatGame.Core.Interfaces;
using CatGame.Features.CharacterSelection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VictoryCondition  : MonoBehaviour
{
    [SerializeField] private CharacterCatalog characterCatalog;

    [Tooltip("Delay depois do final da mensagem de TimeOver para a UI de vitória aparecer.")]
    [SerializeField] private float delayToShowVictoryUI;
    
    [Tooltip("Delay depois da UI de vitória aparecer para a UI de final de partida aparecer.")]
    [SerializeField] private float delayToShowEndUI;

    private IPlayerRegistry playerRegistry;
    
    private void Start()
    {
        playerRegistry = ServiceLocator.Get<IPlayerRegistry>();

        GameManager.Instance.OnPlayerLost += GameManager_OnPlayerLost;
        GameManager.Instance.OnGameStateChanged += GameManager_OnGameStateChanged;
        ScoreManager.Instance.OnScoreChanged += ScoreManager_OnScoreChanged;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.OnPlayerLost -= GameManager_OnPlayerLost;
            GameManager.Instance.OnGameStateChanged -= GameManager_OnGameStateChanged;
        }

        if (ScoreManager.Instance)
        {
            ScoreManager.Instance.OnScoreChanged -= ScoreManager_OnScoreChanged;
        }
    }

    private void GameManager_OnPlayerLost(PlayerId playerID)
    {
        EventBus.Publish(new PlayerDefeatedEvent(playerID));
    }

    private void GameManager_OnGameStateChanged(GameManager.GameState currentState)
    {
        switch (currentState)
        {
            case GameManager.GameState.GameOver:
                StartCoroutine(WaitToShowVictoryUI());
                break;
        }
    }

    private IEnumerator WaitToShowVictoryUI()
    {
        // Dá tempo do RPC da mensagem do servidor chegar e marcar IsShowing = true.
        // RESOLVER DEPOIS - VAI DAR RUIM NA APRESENTACAO
        // PORRA GABRIEL, ME AJUDA A TE AJUDAR, PFVVVVVVVVVVVVVVVVVVVVV
        yield return new WaitForSecondsRealtime(0f);

        // Espera a mensagem do servidor terminar antes de exibir a animação de vitória.
        yield return new WaitUntil(() => ServerMessageManager.Instance == null || !ServerMessageManager.Instance.IsShowing);

        // Delay para a mensagem aparecer.
        yield return new WaitForSeconds(delayToShowVictoryUI);

        GetWinner();
        StartCoroutine(WaitToShowEndUI());
    }

    private IEnumerator WaitToShowEndUI()
    {
        yield return new WaitForSeconds(delayToShowEndUI);

        // Cada máquina abre a própria tela de fim de partida.
        ServiceLocator.Get<IUIService>().Push(PanelType.MatchEnd);
    }

    private void ScoreManager_OnScoreChanged(ScoreManager.ScoreEntry changeEvent)
    {
        // Envia o scoreEntry.
        EventBus.Publish(new ScoreChangedEvent()
        {
            PlayerIndex = changeEvent.playerId.Id,
            ScoreAmount = changeEvent.score,
        });

        List<int> bestPlayer = new();
        uint bestScore = 0;

        // Calcula quem est� vencendo.
        foreach (var score in ScoreManager.Instance.ScoreEntrys)
        {
            // Se o player perdeu antes do jogo terminar, n�o participa da contagem.
            if (GameManager.Instance.PlayerLost.Contains(score.playerId))
                continue;

            if (score.score > bestScore)
            {
                bestScore = score.score;
                bestPlayer.Clear();
                bestPlayer.Add(score.playerId.Id);
            }
            else if (score.score == bestScore)
            {
                bestPlayer.Add(score.playerId.Id);
            }
        }

        // Avisa � todos quem est� vencendo
        foreach (ScoreManager.ScoreEntry scoreEntry in ScoreManager.Instance.ScoreEntrys)
        {
            EventBus.Publish(new LeaderChangedEvent()
            {
                PlayerIndex = scoreEntry.playerId.Id,
                IsLeader = bestPlayer.Contains(scoreEntry.playerId.Id),
            });
        }
    }

    private void ComputeWinner()
    {

    }

    private void GetWinner()
    {
        PlayerId mvp = PlayerId.None;
        int mvpScore = -1;

        // Itera sobre os player que est�o dentro do jogo
        foreach (PlayerInfo player in playerRegistry.Players)
        {
            PlayerId playerId = player.playerId;

            // Se o player perdeu, ignora na contagem final.
            if (GameManager.Instance.PlayerLost.Contains(playerId))
                continue;

            int scoreIndex = ScoreManager.Instance.ScoreEntrys.FindIndex(x => x.playerId == playerId);

            int playerScore = scoreIndex >= 0
                ? (int)ScoreManager.Instance.ScoreEntrys[scoreIndex].score
                : 0; // Se o player n�o tiver score, deixa ele com 0 para a verifica��o.

            // Pega o player com a maior pontua��o sobre os players restantes.
            if (playerScore > mvpScore)
            {
                mvpScore = playerScore;
                mvp = playerId;
            }            
        }

        // Pega o mvp.
        if (!playerRegistry.TryGetPlayerInfo(mvp, out PlayerInfo mvpInfo))
            return;

        if (!characterCatalog.TryGet(mvpInfo.characterId, out CharacterCatalog.Entry catalogEntry))
            return;

        // Anuncia o vencedor.
        EventBus.Publish(new PlayerWonEvent(catalogEntry.Portrait, mvp));
    }
}
