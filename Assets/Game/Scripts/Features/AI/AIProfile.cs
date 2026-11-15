using UnityEngine;

namespace CatGame.Features.AI
{
    /// <summary>
    /// Configuração de dificuldade e estilo de jogo de uma IA.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/AI Profile", fileName = "New AI Profile")]
    public class AIProfile : ScriptableObject
    {
        [Header("Movement")]
        [Tooltip("Velocidade de movimento da IA (unidades por segundo).")]
        [SerializeField, Min(0f)] private float moveSpeed = 5f;

        [Tooltip("Distância até o alvo em que a IA considera que chegou.")]
        [SerializeField, Min(0.001f)] private float arriveTolerance = 0.05f;

        [Header("Health")]
        [Tooltip("Vida inicial da IA.")]
        [SerializeField, Min(1f)] private float initialHealth = 3f;

        [Header("Timing")]
        [Tooltip("Tempo de reação mínimo antes de decidir onde soltar (0 = imediato).")]
        [SerializeField, Min(0f)] private float minReactionTime = 0.2f;

        [Tooltip("Tempo de reação máximo. O tempo real é sorteado entre o mínimo e o máximo.")]
        [SerializeField, Min(0f)] private float maxReactionTime = 0.5f;

        [Tooltip("Intervalo para reavaliar o alvo enquanto anda.")]
        [SerializeField, Min(0.05f)] private float replanInterval = 0.5f;

        [Tooltip("Quanto uma nova jogada precisa ser melhor que a atual para a IA trocar de alvo.")]
        [SerializeField, Min(0f)] private float replanThreshold = 0.5f;

        [Tooltip("Espera as frutas do pote pararem de se mover antes de decidir.")]
        [SerializeField] private bool waitForSettle = true;

        [Tooltip("Velocidade abaixo da qual uma fruta é considerada parada.")]
        [SerializeField, Min(0f)] private float settleSpeed = 0.3f;

        [Tooltip("Tempo máximo esperando as frutas pararem.")]
        [SerializeField, Min(0f)] private float maxSettleWait = 1.5f;

        [Header("Perception")]
        [Tooltip("Considera a próxima fruta ao decidir a jogada atual.")]
        [SerializeField] private bool useNextFruit = true;

        [Tooltip("Quantas posições de drop são testadas ao longo do pote.")]
        [SerializeField, Range(5, 60)] private int candidateCount = 25;

        [Header("Mistakes (0 = perfeita)")]
        [Tooltip("Desvio máximo da mira, em unidades.")]
        [SerializeField, Min(0f)] private float aimError = 0f;

        [Tooltip("Chance de não escolher a melhor jogada.")]
        [SerializeField, Range(0f, 1f)] private float mistakeChance = 0f;

        [Tooltip("Quando erra, sorteia entre as N melhores jogadas (1 = sempre a melhor).")]
        [SerializeField, Min(1)] private int topChoices = 1;

        [Header("Weights")]
        [Tooltip("Valor de juntar com uma fruta igual.")]
        [SerializeField] private float mergeWeight = 10f;

        [Tooltip("Bônus por tier nas junções (juntar frutas maiores vale mais).")]
        [SerializeField] private float tierWeight = 0.5f;

        [Tooltip("Valor de a fruta resultante encostar em outra do tier seguinte (cadeia).")]
        [SerializeField] private float chainWeight = 4f;

        [Tooltip("Penalidade por deixar o pote alto.")]
        [SerializeField] private float heightWeight = 6f;

        [Tooltip("Penalidade por colocar uma fruta grande em cima de frutas menores.")]
        [SerializeField] private float buryWeight = 2f;

        [Tooltip("Valor de encostar em frutas de tier vizinho.")]
        [SerializeField] private float neighborWeight = 1f;

        [Tooltip("Valor de deixar frutas grandes nas laterais do pote.")]
        [SerializeField] private float wallWeight = 1f;

        [Tooltip("Peso da melhor jogada da próxima fruta na decisão atual.")]
        [SerializeField, Min(0f)] private float nextFruitWeight = 0.5f;

        [Tooltip("Penalidade por passar da linha de derrota.")]
        [SerializeField, Min(0f)] private float dangerPenalty = 50f;

        [Header("Emergency")]
        [Tooltip("Muda a prioridade para baixar o pote quando ele está perto de encher. Desligado, a IA pode perder por altura.")]
        [SerializeField] private bool useEmergency = true;

        [Tooltip("Fração da altura até a linha de derrota que ativa a emergência.")]
        [SerializeField, Range(0f, 1f)] private float emergencyHeight = 0.8f;

        [Tooltip("Fração do tempo para perder vida que ativa a emergência.")]
        [SerializeField, Range(0f, 1f)] private float emergencyTime = 0.3f;

        [Tooltip("Multiplicador da penalidade de altura e do valor de juntar frutas em perigo durante a emergência.")]
        [SerializeField, Min(1f)] private float emergencyMultiplier = 3f;

        [Header("Debug")]
        [Tooltip("Desenha as posições testadas e o alvo na Scene.")]
        [SerializeField] private bool drawGizmos;

        public float MoveSpeed => moveSpeed;
        public float ArriveTolerance => arriveTolerance;
        public float InitialHealth => initialHealth;

        public float MinReactionTime => minReactionTime;
        public float MaxReactionTime => maxReactionTime;
        public float ReplanInterval => replanInterval;
        public float ReplanThreshold => replanThreshold;
        public bool WaitForSettle => waitForSettle;
        public float SettleSpeed => settleSpeed;
        public float MaxSettleWait => maxSettleWait;

        public bool UseNextFruit => useNextFruit;
        public int CandidateCount => candidateCount;

        public float AimError => aimError;
        public float MistakeChance => mistakeChance;
        public int TopChoices => topChoices;

        public float MergeWeight => mergeWeight;
        public float TierWeight => tierWeight;
        public float ChainWeight => chainWeight;
        public float HeightWeight => heightWeight;
        public float BuryWeight => buryWeight;
        public float NeighborWeight => neighborWeight;
        public float WallWeight => wallWeight;
        public float NextFruitWeight => nextFruitWeight;
        public float DangerPenalty => dangerPenalty;

        public bool UseEmergency => useEmergency;
        public float EmergencyHeight => emergencyHeight;
        public float EmergencyTime => emergencyTime;
        public float EmergencyMultiplier => emergencyMultiplier;

        public bool DrawGizmos => drawGizmos;

        private void OnValidate()
        {
            if (maxReactionTime < minReactionTime)
                maxReactionTime = minReactionTime;
        }
    }
}
