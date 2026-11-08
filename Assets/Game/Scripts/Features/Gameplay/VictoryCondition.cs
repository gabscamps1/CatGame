using CatGame.Core;
using CatGame.Core.Data;
using CatGame.Core.Events;
using CatGame.Core.Interfaces;
using CatGame.Features.CharacterSelection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VictoryCondition : MonoBehaviour
{
    [SerializeField] private CharacterCatalog characterCatalog;

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

                /*// Partida foi encerrada pois o tempo acabou.
                if (Timer.CurrentTimer <= 0)
                {
                    StartCoroutine(WaitToShowVictory());
                }

                // Partida foi encerrada porque somente sobrou um jogador vivo.
                else
                {
                    GetWinner();
                }*/

                GetWinner();
                break;
        }
    }

    private IEnumerator WaitToShowVictory()
    {
        // Espera a mensagem do servidor terminar antes de exibir a animação de vitória.
        yield return new WaitUntil(() => !ServerMessageManager.Instance.IsShowing);

        GetWinner();
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

        // Calcula quem está vencendo.
        foreach (var score in ScoreManager.Instance.ScoreEntrys)
        {
            // Se o player perdeu antes do jogo terminar, não participa da contagem.
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

        // Avisa à todos quem está vencendo
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

        // Itera sobre os player que estão dentro do jogo
        foreach (PlayerInfo player in playerRegistry.Players)
        {
            PlayerId playerId = player.playerId;

            // Se o player perdeu, ignora na contagem final.
            if (GameManager.Instance.PlayerLost.Contains(playerId))
                continue;

            // Pega o player com a maior pontuação sobre os players restantes.
            ScoreManager.ScoreEntry scoreEntry = ScoreManager.Instance.ScoreEntrys.Find(x => x.playerId == playerId);

            int playerScore = scoreEntry != null 
                ? (int)scoreEntry.score 
                : 0; // Se o player não tiver score, deixa ele com 0 para a verificação.

            if (playerScore > mvpScore)
            {
                mvpScore = playerScore;
                mvp = playerId;
            }            
        }

        if (mvp.Id >= MatchSetup.Players.Count)
            return;

        ushort characterId = MatchSetup.Players[mvp.Id].CharacterId;

        if (!characterCatalog.TryGet(characterId, out CharacterCatalog.Entry catalogEntry))
            return;

        // Anuncia o vencedor.
        EventBus.Publish(new PlayerWonEvent(catalogEntry.Portrait, mvp));
    }
}
