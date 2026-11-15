using System.Collections.Generic;
using UnityEngine;

namespace CatGame.Features.AI.Planning
{
    /// <summary>
    /// Dá uma nota a um drop simulado, usando os pesos do perfil da IA.
    /// </summary>
    public static class DropEvaluator
    {
        public static float Evaluate(GlassSnapshot snapshot, in DropResult result, List<int> contacts, AIProfile profile, bool isEmergency)
        {
            float maxTier = Mathf.Max(1, snapshot.MaxTier);
            float score = 0f;
            float resultTop = result.top;

            if (result.mergeIndex >= 0)
            {
                SnapshotFruit other = snapshot.Fruits[result.mergeIndex];
                byte mergedTier = (byte)(result.tier + 1);
                float mergedRadius = snapshot.GetRadius(mergedTier);
                Vector2 mergedPosition = DropSimulator.GetMergePosition(snapshot, result.landing, other.position, mergedRadius);
                resultTop = mergedPosition.y + mergedRadius;

                // Juntar.
                score += profile.MergeWeight * (1f + profile.TierWeight * result.tier);

                // Cadeia: a fruta resultante encosta em outra do tier seguinte.
                if (mergedTier < snapshot.MaxTier)
                {
                    for (int i = 0; i < snapshot.Fruits.Count; i++)
                    {
                        if (i == result.mergeIndex)
                            continue;

                        SnapshotFruit fruit = snapshot.Fruits[i];

                        if (fruit.tier != mergedTier || !DropSimulator.AreTouching(mergedPosition, mergedRadius, fruit.position, fruit.radius))
                            continue;

                        score += profile.ChainWeight * (1f + profile.TierWeight * mergedTier);
                        break;
                    }
                }

                // Emergência: juntar uma fruta que já está na zona de derrota a tira de lá.
                if (isEmergency)
                    score += profile.MergeWeight * profile.EmergencyMultiplier * other.dangerTime;
            }
            else
            {
                foreach (int index in contacts)
                {
                    SnapshotFruit fruit = snapshot.Fruits[index];
                    int tierDifference = result.tier - fruit.tier;

                    // Vizinhança: tiers próximos ficam juntos.
                    if (Mathf.Abs(tierDifference) == 1)
                        score += profile.NeighborWeight;

                    // Enterrar: fruta grande em cima de uma menor.
                    if (tierDifference > 0 && fruit.position.y < result.landing.y)
                        score -= profile.BuryWeight * tierDifference / maxTier;
                }
            }

            // Paredes: frutas grandes nas laterais.
            float wallDistance = Mathf.Min(
                result.landing.x - (snapshot.InnerLeftX + result.radius),
                (snapshot.InnerRightX - result.radius) - result.landing.x);

            if (wallDistance <= result.radius * 0.5f)
                score += profile.WallWeight * (result.tier / maxTier);

            // Altura.
            float heightRatio = Mathf.Clamp01((resultTop - snapshot.FloorY) / snapshot.GlassHeight);
            float heightWeight = profile.HeightWeight * (isEmergency ? profile.EmergencyMultiplier : 1f);
            score -= heightWeight * heightRatio * heightRatio;

            if (resultTop >= snapshot.DangerLineY)
                score -= profile.DangerPenalty;

            return score;
        }
    }
}
