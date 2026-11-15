using System.Collections.Generic;
using UnityEngine;

namespace CatGame.Features.AI.Planning
{
    /// <summary>
    /// Resultado de soltar uma fruta em uma posição X.
    /// </summary>
    public struct DropResult
    {
        public float x;
        public byte tier;
        public float radius;
        public Vector2 landing; // Centro da fruta onde ela para.
        public float top;       // Topo da fruta onde ela para.
        public int mergeIndex;  // Índice da fruta igual em que ela encosta (-1 = nenhuma).
    }

    /// <summary>
    /// Modelo geométrico simples: a fruta cai reto e para no chão ou na primeira fruta em que encostar.
    /// Não prevê a fruta rolando depois de encostar.
    /// </summary>
    public static class DropSimulator
    {
        private const float ContactTolerance = 0.05f; // Folga de 5% sobre a soma dos raios para considerar contato.

        public static DropResult Simulate(GlassSnapshot snapshot, float x, byte tier, List<int> contacts)
        {
            float radius = snapshot.GetRadius(tier);
            float landingY = snapshot.FloorY + radius;

            List<SnapshotFruit> fruits = snapshot.Fruits;

            // Ponto mais alto em que a fruta encosta, caindo reto.
            for (int i = 0; i < fruits.Count; i++)
            {
                SnapshotFruit fruit = fruits[i];
                float distanceX = x - fruit.position.x;
                float radiusSum = radius + fruit.radius;

                if (Mathf.Abs(distanceX) >= radiusSum)
                    continue;

                float y = fruit.position.y + Mathf.Sqrt(radiusSum * radiusSum - distanceX * distanceX);

                if (y > landingY)
                    landingY = y;
            }

            Vector2 landing = new(x, landingY);

            contacts.Clear();
            int mergeIndex = -1;

            for (int i = 0; i < fruits.Count; i++)
            {
                SnapshotFruit fruit = fruits[i];

                if (!AreTouching(landing, radius, fruit.position, fruit.radius))
                    continue;

                contacts.Add(i);

                if (mergeIndex < 0 && fruit.tier == tier && tier < snapshot.MaxTier)
                    mergeIndex = i;
            }

            return new DropResult
            {
                x = x,
                tier = tier,
                radius = radius,
                landing = landing,
                top = landingY + radius,
                mergeIndex = mergeIndex
            };
        }

        public static bool AreTouching(Vector2 positionA, float radiusA, Vector2 positionB, float radiusB)
        {
            float limit = (radiusA + radiusB) * (1f + ContactTolerance);
            return (positionA - positionB).sqrMagnitude <= limit * limit;
        }

        /// <summary>
        /// Onde nasce a fruta resultante de uma junção (ponto médio, dentro do pote).
        /// </summary>
        public static Vector2 GetMergePosition(GlassSnapshot snapshot, Vector2 positionA, Vector2 positionB, float mergedRadius)
        {
            Vector2 middle = (positionA + positionB) * 0.5f;
            middle.x = Mathf.Clamp(middle.x, snapshot.InnerLeftX + mergedRadius, snapshot.InnerRightX - mergedRadius);
            middle.y = Mathf.Max(middle.y, snapshot.FloorY + mergedRadius);
            return middle;
        }

        /// <summary>
        /// Gera a foto do pote depois do drop (com a junção, se houver) e passa a próxima fruta para a mão.
        /// </summary>
        public static GlassSnapshot Apply(GlassSnapshot snapshot, in DropResult result)
        {
            GlassSnapshot next = snapshot.Clone();

            if (result.mergeIndex >= 0)
            {
                SnapshotFruit other = next.Fruits[result.mergeIndex];
                next.Fruits.RemoveAt(result.mergeIndex);

                byte mergedTier = (byte)(result.tier + 1);
                float mergedRadius = next.GetRadius(mergedTier);
                Vector2 mergedPosition = GetMergePosition(next, result.landing, other.position, mergedRadius);

                next.Fruits.Add(new SnapshotFruit(mergedPosition, mergedRadius, mergedTier, 0f));
            }
            else
            {
                next.Fruits.Add(new SnapshotFruit(result.landing, result.radius, result.tier, 0f));
            }

            next.HeldTier = snapshot.NextTier;
            next.NextTier = 0;
            return next;
        }
    }
}
