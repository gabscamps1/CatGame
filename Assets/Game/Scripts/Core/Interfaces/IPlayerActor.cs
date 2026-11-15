using CatGame.Capabilities.Health;
using CatGame.Core.Data;
using System;

namespace CatGame.Core.Interfaces
{
    /// <summary>
    /// Contrato de um participante da partida (jogador ou IA).
    /// </summary>
    public interface IPlayerActor
    {
        public PlayerId PlayerId { get; }

        /// <summary>
        /// Verdadeiro depois que o participante nasceu na rede e já tem um PlayerId configurado.
        /// </summary>
        public bool IsReady { get; }

        public IDamageable Damageable { get; }
        public PlayerFruitHand Hand { get; }

        /// <summary>
        /// Disparado quando o PlayerId é aplicado ou alterado.
        /// </summary>
        public event Action<PlayerId> PlayerIdChanged;

        /// <summary>
        /// Define o PlayerId antes do spawn (ou depois, no server).
        /// </summary>
        public void SetPlayerId(PlayerId newPlayerId);
    }
}
