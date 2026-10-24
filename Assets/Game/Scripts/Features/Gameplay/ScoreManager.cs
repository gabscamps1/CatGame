using CatGame.Core.Data;
using CatGame.Core.Events;
using System;
using System.Diagnostics;
using Unity.Netcode;

public class ScoreManager : NetworkBehaviour
{
    public struct Score : INetworkSerializable, IEquatable<Score>
    {
        public PlayerId playerId;
        public uint score; 
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref playerId);
            serializer.SerializeValue(ref score);
        }

        public bool Equals(Score other) => playerId == other.playerId && score == other.score;
    }

    public static ScoreManager Instance;

    private readonly NetworkList<Score> scores = new();

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
        scores.OnListChanged += Scores_OnListChanged;
    }

    public override void OnNetworkDespawn()
    {
        scores.OnListChanged -= Scores_OnListChanged;
    }

    public void IncreaseScore(PlayerId playerId, int score)
    {
        if (!IsServer || !IsSpawned || score < 0) 
            return;

        int index = -1;

        for (int i = 0; i < scores.Count; i++)
        {
            if (scores[i].playerId == playerId)
            {
                index = i;
            }
        }

        if (index < 0)
        {
            scores.Add(new Score
            {
                playerId = playerId,
                score = (uint)score
            });

            return;
        }

        Score newScore = scores[index];
        newScore.score += (uint)score;
        scores[index] = newScore;
    }

    private void Scores_OnListChanged(NetworkListEvent<Score> changeEvent)
    {
        CatGame.Core.Logger.Log($"Enviou a informação pelo eventbus");
        EventBus.Publish(new ScoreChangedEvent()
        {
            PlayerIndex = changeEvent.Value.playerId.Id,
            ScoreAmount = changeEvent.Value.score,
        });
    }
}