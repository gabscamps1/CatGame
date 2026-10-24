using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Fruit Catalog")]
public sealed class FruitCatalog : ScriptableObject
{
    [Serializable]
    public struct TierData
    {
        public Sprite sprite;
        public float diameter; // Tamanho da fruta.
        public float mass;
        public int score;
    }

    [SerializeField] private TierData[] tiers; // posição 0 = tier 1
    [SerializeField] private int maxStartTier = 4; // Tier máximo das frutas que podem aparecer para o drop

    public int MaxTier => tiers.Length;
    public TierData Get(byte tier) => tiers[tier - 1];

    public byte RandomStartTier() => (byte)UnityEngine.Random.Range(1, Mathf.Min(maxStartTier, tiers.Length) + 1);
}