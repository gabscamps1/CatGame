using CatGame.Core.Data;
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
        public override bool Equals(object obj) => obj is ScoreEntry other && Equals(other);

        public static bool operator ==(ScoreEntry left, ScoreEntry right) => left.Equals(right);
        public static bool operator !=(ScoreEntry left, ScoreEntry right) => !left.Equals(right);

        public override int GetHashCode()
        {
            return playerId.Id;
        }
    }

    public static ScoreManager Instance;

    public readonly List<ScoreEntry> ScoreEntrys = new();
    private readonly NetworkList<ScoreEntry> scoreEntrys = new();

    public event Action<ScoreEntry> OnScoreChanged;

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
        ScoreEntrys.Clear();

        foreach (ScoreEntry entry in scoreEntrys)
            ScoreEntrys.Add(entry);

        switch (changeEvent.Type)
        {
            // Eventos que trazem o novo score de um jogador.
            case NetworkListEvent<ScoreEntry>.EventType.Add:
            case NetworkListEvent<ScoreEntry>.EventType.Insert:
            case NetworkListEvent<ScoreEntry>.EventType.Value:
                OnScoreChanged?.Invoke(changeEvent.Value);
                break;

            // Eventos de remoções e sincronização completa, avisa o score atual de todos.
            default:
                foreach (ScoreEntry entry in ScoreEntrys)
                    OnScoreChanged?.Invoke(entry);
                break;
        }
    }
}