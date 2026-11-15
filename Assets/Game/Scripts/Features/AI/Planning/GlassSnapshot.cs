using System.Collections.Generic;
using UnityEngine;

namespace CatGame.Features.AI.Planning
{
    /// <summary>
    /// Uma fruta dentro da foto do pote.
    /// </summary>
    public readonly struct SnapshotFruit
    {
        public readonly Vector2 position;
        public readonly float radius;
        public readonly byte tier;
        public readonly float dangerTime; // Fração do tempo para perder vida já passada na zona de derrota (0 a 1).

        public SnapshotFruit(Vector2 position, float radius, byte tier, float dangerTime)
        {
            this.position = position;
            this.radius = radius;
            this.tier = tier;
            this.dangerTime = dangerTime;
        }
    }

    /// <summary>
    /// Cópia em dados do pote de uma IA no instante da decisão.
    /// Permite testar jogadas sem mexer nas frutas reais do jogo.
    /// </summary>
    public class GlassSnapshot
    {
        private readonly float[] tierRadius; // Índice = tier (o índice 0 não é usado).

        public List<SnapshotFruit> Fruits { get; } = new();

        public float InnerLeftX { get; set; }
        public float InnerRightX { get; set; }
        public float FloorY { get; set; }
        public float DangerLineY { get; set; }

        // Alcance do ponto de drop (área de movimento do jogador).
        public float MinDropX { get; set; }
        public float MaxDropX { get; set; }

        public byte HeldTier { get; set; } // 0 = mão vazia
        public byte NextTier { get; set; } // 0 = desconhecida ou ignorada

        public int MaxTier => tierRadius.Length - 1;
        public float GlassHeight => Mathf.Max(0.01f, DangerLineY - FloorY);

        public GlassSnapshot(float[] tierRadius)
        {
            this.tierRadius = tierRadius;
        }

        public float GetRadius(byte tier)
        {
            return tierRadius[Mathf.Clamp(tier, 1, MaxTier)];
        }

        public GlassSnapshot Clone()
        {
            GlassSnapshot copy = new(tierRadius)
            {
                InnerLeftX = InnerLeftX,
                InnerRightX = InnerRightX,
                FloorY = FloorY,
                DangerLineY = DangerLineY,
                MinDropX = MinDropX,
                MaxDropX = MaxDropX,
                HeldTier = HeldTier,
                NextTier = NextTier
            };

            copy.Fruits.AddRange(Fruits);
            return copy;
        }
    }
}
