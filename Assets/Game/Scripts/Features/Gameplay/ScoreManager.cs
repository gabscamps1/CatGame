using CatGame.Core.Data;
using CatGame.Core.Events;
using System;
using System.Collections.Generic;
using Unity.Netcode;

public class ScoreManager : NetworkBehaviour
{
    public struct ScoreEntry : INetworkSerializable, IEquatable<ScoreEntry>
    {
        public PlayerId playerId;
        public uint score; 
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref playerId);
            serializer.SerializeValue(ref score);
        }

        public bool Equals(ScoreEntry other) => playerId == other.playerId && score == other.score;
    }

    public static ScoreManager Instance;

    private readonly NetworkList<ScoreEntry> scoreEntrys = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }
    }

    public override void OnNetworkSpawn()
    {
        scoreEntrys.OnListChanged += Scores_OnListChanged;
    }

    public override void OnNetworkDespawn()
    {
        scoreEntrys.OnListChanged -= Scores_OnListChanged;
    }

    public void IncreaseScore(PlayerId playerId, int score)
    {
        if (!IsServer || !IsSpawned || score < 0) 
            return;

        int index = -1;

        for (int i = 0; i < scoreEntrys.Count; i++)
        {
            if (scoreEntrys[i].playerId == playerId)
            {
                index = i;
            }
        }

        if (index < 0)
        {
            scoreEntrys.Add(new ScoreEntry
            {
                playerId = playerId,
                score = (uint)score
            });

            return;
        }

        ScoreEntry newScore = scoreEntrys[index];
        newScore.score += (uint)score;
        scoreEntrys[index] = newScore;
    }

    private void Scores_OnListChanged(NetworkListEvent<ScoreEntry> changeEvent)
    {
        // Envia o scoreEntry.
        EventBus.Publish(new ScoreChangedEvent()
        {
            PlayerIndex = changeEvent.Value.playerId.Id,
            ScoreAmount = changeEvent.Value.score,
        });

        List<int> bestPlayer = new();
        uint bestScore = 0;

        // Calcula quem está vencendo
        foreach (var score in scoreEntrys)
        {      
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
        foreach (ScoreEntry scoreEntry in scoreEntrys)
        {
            EventBus.Publish(new LeaderChangedEvent()
            {
                PlayerIndex = scoreEntry.playerId.Id,
                IsLeader = bestPlayer.Contains(scoreEntry.playerId.Id),
            });
        }
    }
}