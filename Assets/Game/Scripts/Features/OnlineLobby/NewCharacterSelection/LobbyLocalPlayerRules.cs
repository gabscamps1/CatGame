using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace CatGame.Features.CharacterSelection
{
    /// <summary>
    /// Regras dos jogadores locais de um client no lobby (sem rede e sem input).
    /// </summary>
    public static class LobbyLocalPlayerRules
    {
        public const byte MAIN_LOCAL_ID = 0;

        /// <summary>
        /// Índice local de um convidado novo: o menor livre a partir de 1 (o 0 é sempre o jogador principal).
        /// -1 quando não há mais jogadores locais disponíveis.
        /// </summary>
        public static int NextGuestLocalId(ICollection<byte> usedLocalIds, int localPlayerCount)
        {
            for (int i = MAIN_LOCAL_ID + 1; i < localPlayerCount; i++)
            {
                if (!usedLocalIds.Contains((byte)i))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Qual vaga em espera um dispositivo livre assume:
        /// 1. A vaga que o último dispositivo é este (o mesmo dispositivo sempre volta para a vaga dele);
        /// 2. Senão, a vaga em espera de menor índice local.
        /// -1 quando este client não tem vaga em espera.
        /// </summary>
        public static int PickWaitingLocalId(InputDevice device, IReadOnlyDictionary<byte, InputDevice> waitingLastDevices)
        {
            int lowest = -1;

            foreach (var pair in waitingLastDevices)
            {
                if (pair.Value == device)
                    return pair.Key;

                if (lowest < 0 || pair.Key < lowest)
                    lowest = pair.Key;
            }

            return lowest;
        }
    }
}
