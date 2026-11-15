using CatGame.Capabilities.Health;
using CatGame.Capabilities.Movement;
using CatGame.Core.Data;
using CatGame.Core.Interfaces;
using CatGame.Features.AI.Planning;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace CatGame.Features.AI
{
    /// <summary>
    /// Controller da IA: decide onde soltar a fruta, move o personagem e solta.
    /// Roda somente no server. Identidade, registro, área de movimento e derrota ficam no PlayerActor.
    /// </summary>
    [RequireComponent(typeof(PlayerActor))]
    public class AIController : NetworkBehaviour
    {
        private enum AIState : byte
        {
            Waiting,  // Esperando a mão ficar pronta e as frutas pararem.
            Thinking, // Tempo de reação antes de decidir.
            Moving    // Andando até a posição escolhida.
        }

        [Header("References")]
        [SerializeField] private FruitCatalog fruitCatalog;

        [Header("Settings")]
        [SerializeField] private AIProfile profile;

        private IPlayerActor actor;
        private CharacterMovement characterMovement;
        private HealthController healthController;
        private PlayerFruitHand hand;

        private DropPlanner planner;
        private AIState state;
        private float reactionTimer;
        private float replanTimer;
        private float settleTimer;
        private float targetX;

        private float[] tierRadius;
        private GlassSnapshot lastSnapshot; // Usado só pelos gizmos.

        public AIProfile Profile => profile;

        private void Awake()
        {
            actor = GetComponent<IPlayerActor>();
            characterMovement = GetComponent<CharacterMovement>();
            healthController = GetComponent<HealthController>();
            hand = GetComponent<PlayerFruitHand>();
        }

        #region Unity Network

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // A IA só pensa no server.
            if (!IsServer)
            {
                enabled = false;
                return;
            }

            planner = new DropPlanner(unchecked((int)NetworkObjectId * 7919 + System.Environment.TickCount));
            ApplyProfile();
            ResetToWaiting();
        }

        #endregion

        /// <summary>
        /// Define o perfil da IA (ex.: dificuldade escolhida no lobby).
        /// </summary>
        public void SetProfile(AIProfile newProfile)
        {
            profile = newProfile;

            if (IsServer && IsSpawned)
                ApplyProfile();
        }

        private void ApplyProfile()
        {
            if (profile == null)
            {
                CatGame.Core.Logger.LogWarning($"[AIController] {name} está sem AIProfile.");
                return;
            }

            characterMovement.SetMoveSpeed(profile.MoveSpeed);
            healthController.SetMaxHealth(profile.InitialHealth);
        }

        private void Update()
        {
            if (!IsServer || !IsSpawned || profile == null || !actor.IsReady)
                return;

            if (GameManager.Instance == null || GameManager.Instance.GetGameState() != GameManager.GameState.Playing)
                return;

            PlayerId playerId = actor.PlayerId;

            if (GameManager.Instance.PlayerLost.Contains(playerId))
                return;

            if (!GlassArea.TryGet(playerId.Id, out GlassArea glassArea))
                return;

            switch (state)
            {
                case AIState.Waiting:
                    UpdateWaiting(playerId);
                    break;

                case AIState.Thinking:
                    UpdateThinking(glassArea);
                    break;

                case AIState.Moving:
                    UpdateMoving(glassArea);
                    break;
            }
        }

        #region States

        private void UpdateWaiting(PlayerId playerId)
        {
            if (!hand.CanDrop)
            {
                settleTimer = 0f;
                return;
            }

            // Espera as frutas pararem (com limite de tempo).
            if (profile.WaitForSettle && !IsSettled(playerId) && settleTimer < profile.MaxSettleWait)
            {
                settleTimer += Time.deltaTime;
                return;
            }

            reactionTimer = planner.NextRange(profile.MinReactionTime, profile.MaxReactionTime);
            state = AIState.Thinking;
        }

        private void UpdateThinking(GlassArea glassArea)
        {
            reactionTimer -= Time.deltaTime;

            if (reactionTimer > 0f)
                return;

            GlassSnapshot snapshot = BuildSnapshot(glassArea);

            if (snapshot == null || !planner.TryPlan(snapshot, profile, out DropCandidate choice))
            {
                ResetToWaiting();
                return;
            }

            targetX = choice.x;
            replanTimer = profile.ReplanInterval;
            state = AIState.Moving;
        }

        private void UpdateMoving(GlassArea glassArea)
        {
            // A mão deixou de estar pronta (ex.: estado mudou): recomeça.
            if (!hand.CanDrop)
            {
                ResetToWaiting();
                return;
            }

            replanTimer -= Time.deltaTime;

            if (replanTimer <= 0f)
            {
                replanTimer = profile.ReplanInterval;
                Replan(glassArea);
            }

            if (!MoveTowards(targetX))
                return;

            hand.RequestDrop();
            ResetToWaiting();
        }

        private void ResetToWaiting()
        {
            state = AIState.Waiting;
            settleTimer = 0f;
        }

        #endregion

        /// <summary>
        /// Reavalia o pote e só troca de alvo se a nova jogada for claramente melhor.
        /// </summary>
        private void Replan(GlassArea glassArea)
        {
            GlassSnapshot snapshot = BuildSnapshot(glassArea);

            if (snapshot == null || !planner.TryPlan(snapshot, profile, out DropCandidate choice))
                return;

            float currentScore = planner.ScoreAt(snapshot, profile, targetX);

            if (choice.score > currentScore + profile.ReplanThreshold)
                targetX = choice.x;
        }

        /// <summary>
        /// Move o ponto de drop até o X alvo. Retorna verdadeiro quando chegou (ou não dá para chegar mais perto).
        /// </summary>
        private bool MoveTowards(float x)
        {
            float currentX = hand.DropPoint.x;
            float distance = x - currentX;

            if (Mathf.Abs(distance) <= profile.ArriveTolerance)
                return true;

            float step = characterMovement.MoveSpeed * Time.deltaTime;

            if (step <= 0f)
                return false;

            // Freia no último passo para não passar do alvo.
            float direction = Mathf.Clamp(distance / step, -1f, 1f);
            characterMovement.Move(new Vector2(direction, 0f), Time.deltaTime);

            // Encostou no limite da área de movimento: não dá para chegar mais perto.
            return Mathf.Approximately(hand.DropPoint.x, currentX);
        }

        private bool IsSettled(PlayerId playerId)
        {
            float maxSpeed = profile.SettleSpeed * profile.SettleSpeed;

            foreach (FruitWorld fruit in FruitWorld.Active)
            {
                if (!fruit.IsSpawned || fruit.PlayerOwner != playerId)
                    continue;

                if (fruit.Velocity.sqrMagnitude > maxSpeed)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Monta a foto do pote desta IA a partir do pote e das frutas reais.
        /// </summary>
        private GlassSnapshot BuildSnapshot(GlassArea glassArea)
        {
            float[] radius = GetTierRadius();

            if (radius == null || glassArea.MovementBounds == null)
                return null;

            Bounds dropBounds = glassArea.MovementBounds.bounds;
            float timeToLose = glassArea.MaxHeight != null ? Mathf.Max(0.01f, glassArea.MaxHeight.TimeToLose) : 0f;

            GlassSnapshot snapshot = new(radius)
            {
                InnerLeftX = glassArea.InnerLeftX,
                InnerRightX = glassArea.InnerRightX,
                FloorY = glassArea.FloorY,
                DangerLineY = glassArea.DangerLineY,
                MinDropX = dropBounds.min.x,
                MaxDropX = dropBounds.max.x,
                HeldTier = hand.CurrentTier,
                NextTier = profile.UseNextFruit ? hand.NextTier : (byte)0
            };

            PlayerId playerId = actor.PlayerId;

            foreach (FruitWorld fruit in FruitWorld.Active)
            {
                if (!fruit.IsSpawned || fruit.IsMerging || fruit.PlayerOwner != playerId)
                    continue;

                float dangerTime = timeToLose > 0f ? Mathf.Clamp01(fruit.TimeInCollision / timeToLose) : 0f;
                snapshot.Fruits.Add(new SnapshotFruit(fruit.Position, fruit.WorldRadius, fruit.Tier, dangerTime));
            }

            lastSnapshot = snapshot;
            return snapshot;
        }

        private float[] GetTierRadius()
        {
            if (tierRadius != null)
                return tierRadius;

            if (fruitCatalog == null || FruitSpawner.Instance == null)
            {
                CatGame.Core.Logger.LogWarning($"[AIController] {name} sem FruitCatalog ou sem FruitSpawner na cena.");
                return null;
            }

            tierRadius = new float[fruitCatalog.MaxTier + 1];

            for (int tier = 1; tier <= fruitCatalog.MaxTier; tier++)
                tierRadius[tier] = FruitSpawner.Instance.GetWorldRadius((byte)tier);

            return tierRadius;
        }

        #region Debug

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || profile == null || !profile.DrawGizmos || planner == null || lastSnapshot == null)
                return;

            IReadOnlyList<DropCandidate> candidates = planner.LastCandidates;

            if (candidates.Count == 0)
                return;

            float bestScore = float.MinValue;
            float worstScore = float.MaxValue;

            foreach (DropCandidate candidate in candidates)
            {
                bestScore = Mathf.Max(bestScore, candidate.score);
                worstScore = Mathf.Min(worstScore, candidate.score);
            }

            float range = Mathf.Max(0.0001f, bestScore - worstScore);
            float markerRadius = lastSnapshot.GetRadius(lastSnapshot.HeldTier) * 0.25f;

            // Posições testadas: vermelho = pior, verde = melhor.
            foreach (DropCandidate candidate in candidates)
            {
                Gizmos.color = Color.Lerp(Color.red, Color.green, (candidate.score - worstScore) / range);
                Gizmos.DrawWireSphere(candidate.landing, markerRadius);
            }

            // Alvo atual.
            if (state == AIState.Moving)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(new Vector3(targetX, lastSnapshot.FloorY), new Vector3(targetX, hand.DropPoint.y));
            }
        }

        #endregion
    }
}
