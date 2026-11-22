using CatGame.Core.Enums;
using System;

namespace CatGame.Features.CharacterSelection
{
    public interface ILobbyService
    {
        #region Events

        public event Action Changed; // UI se atualiza quando dispara
        public event Action OnNetworkSpawned; // Quando o lobby é criado na rede nesta máquina.
        public event Action OnOwnerDisconected;
        public event Action<byte, SessionCallbackReason> OnJoinRejected; // Quando está máquina é rejeitada na conexão com o lobby.

        #endregion

        #region State

        public int SlotCount { get; }
        public bool IsHost { get; } // Esta máquina é o host do lobby.
        public string Port { get; }

        public LobbySlot GetSlot(SlotId slot);
        public bool CanControl(SlotId slot); // Esta máquina pode mexer nesta vaga?
        public bool TryGetLocalSlot(byte localId, out SlotId slot); // Vaga do jogador local desta máquina.

        public void Init(int maxPlayers);

        #endregion

        #region Requests

        public void RequestJoin(byte localId, DeviceKind deviceKind);
        public void LeaveSlot(SlotId slot); // Só convidados (índice local diferente de 0).
        public void SetWaitingDevice(SlotId slot); // A vaga fica aguardando dispositivo (perde o ready).
        public void ResumeWithDevice(SlotId slot, DeviceKind deviceKind); // Um dispositivo assumiu a vaga em espera.

        public void CycleCharacter(SlotId slot, int direction);   // -1 ou +1
        public void SetReady(SlotId slot, bool ready);
        public void RequestStart(SlotId slot); // Só o jogador local 0 do host.

        public void RequestCloseLobby();

        #endregion
    }
}
