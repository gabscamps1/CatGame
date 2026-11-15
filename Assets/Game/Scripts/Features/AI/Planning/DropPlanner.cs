using System.Collections.Generic;
using UnityEngine;

namespace CatGame.Features.AI.Planning
{
    /// <summary>
    /// Uma posição de drop testada e a nota dela.
    /// </summary>
    public struct DropCandidate
    {
        public float x;
        public float score;
        public Vector2 landing;

        public DropCandidate(float x, float score, Vector2 landing)
        {
            this.x = x;
            this.score = score;
            this.landing = landing;
        }
    }

    /// <summary>
    /// Testa várias posições de drop, considera a próxima fruta e aplica os erros do perfil.
    /// </summary>
    public class DropPlanner
    {
        private const float TieBreakNoise = 0.01f; // Desempata jogadas iguais (ex.: pote vazio), para IAs diferentes não jogarem igual.
        private const int MinNextCandidates = 5;

        private readonly System.Random random;
        private readonly List<DropCandidate> candidates = new();
        private readonly List<int> contacts = new();
        private readonly List<int> nextContacts = new();

        public IReadOnlyList<DropCandidate> LastCandidates => candidates;

        public DropPlanner(int seed)
        {
            random = new System.Random(seed);
        }

        public float NextRange(float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }

        public bool TryPlan(GlassSnapshot snapshot, AIProfile profile, out DropCandidate choice)
        {
            choice = default;
            candidates.Clear();

            if (snapshot.HeldTier == 0)
                return false;

            bool isEmergency = IsEmergency(snapshot, profile);
            GetDropRange(snapshot, snapshot.HeldTier, out float minX, out float maxX);

            int count = Mathf.Max(1, profile.CandidateCount);

            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : i / (float)(count - 1);
                float x = Mathf.Lerp(minX, maxX, t);

                DropResult result = DropSimulator.Simulate(snapshot, x, snapshot.HeldTier, contacts);
                float score = ScoreResult(snapshot, profile, result, isEmergency) + NextRange(0f, TieBreakNoise);

                candidates.Add(new DropCandidate(x, score, result.landing));
            }

            candidates.Sort((a, b) => b.score.CompareTo(a.score));

            // Erro: às vezes escolhe entre as N melhores em vez da melhor.
            int index = 0;

            if (profile.TopChoices > 1 && candidates.Count > 1 && random.NextDouble() < profile.MistakeChance)
                index = random.Next(1, Mathf.Min(profile.TopChoices, candidates.Count));

            choice = candidates[index];

            // Erro de mira.
            if (profile.AimError > 0f)
                choice.x = Mathf.Clamp(choice.x + NextRange(-profile.AimError, profile.AimError), minX, maxX);

            return true;
        }

        /// <summary>
        /// Nota de soltar a fruta da mão em uma posição X (sem erros), para comparar com o alvo atual.
        /// </summary>
        public float ScoreAt(GlassSnapshot snapshot, AIProfile profile, float x)
        {
            if (snapshot.HeldTier == 0)
                return float.MinValue;

            bool isEmergency = IsEmergency(snapshot, profile);
            DropResult result = DropSimulator.Simulate(snapshot, x, snapshot.HeldTier, contacts);
            return ScoreResult(snapshot, profile, result, isEmergency);
        }

        private float ScoreResult(GlassSnapshot snapshot, AIProfile profile, in DropResult result, bool isEmergency)
        {
            float score = DropEvaluator.Evaluate(snapshot, result, contacts, profile, isEmergency);

            if (profile.UseNextFruit && snapshot.NextTier != 0 && profile.NextFruitWeight > 0f)
                score += profile.NextFruitWeight * BestNextScore(snapshot, profile, result, isEmergency);

            return score;
        }

        /// <summary>
        /// Melhor nota possível para a próxima fruta, depois deste drop.
        /// </summary>
        private float BestNextScore(GlassSnapshot snapshot, AIProfile profile, in DropResult result, bool isEmergency)
        {
            GlassSnapshot next = DropSimulator.Apply(snapshot, result);
            GetDropRange(next, next.HeldTier, out float minX, out float maxX);

            int count = Mathf.Max(MinNextCandidates, profile.CandidateCount / 2);
            float best = float.MinValue;

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)(count - 1);
                DropResult nextResult = DropSimulator.Simulate(next, Mathf.Lerp(minX, maxX, t), next.HeldTier, nextContacts);
                float score = DropEvaluator.Evaluate(next, nextResult, nextContacts, profile, isEmergency);

                if (score > best)
                    best = score;
            }

            return best;
        }

        private static void GetDropRange(GlassSnapshot snapshot, byte tier, out float minX, out float maxX)
        {
            float radius = snapshot.GetRadius(tier);

            minX = Mathf.Max(snapshot.InnerLeftX + radius, snapshot.MinDropX);
            maxX = Mathf.Min(snapshot.InnerRightX - radius, snapshot.MaxDropX);

            if (maxX < minX)
            {
                float center = (minX + maxX) * 0.5f;
                minX = center;
                maxX = center;
            }
        }

        private static bool IsEmergency(GlassSnapshot snapshot, AIProfile profile)
        {
            if (!profile.UseEmergency)
                return false;

            float limitY = snapshot.FloorY + snapshot.GlassHeight * profile.EmergencyHeight;

            foreach (SnapshotFruit fruit in snapshot.Fruits)
            {
                if (fruit.dangerTime > 0f && fruit.dangerTime >= profile.EmergencyTime)
                    return true;

                if (fruit.position.y + fruit.radius >= limitY)
                    return true;
            }

            return false;
        }
    }
}
