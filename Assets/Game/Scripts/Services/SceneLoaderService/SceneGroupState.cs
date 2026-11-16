using CatGame.Core.Enums;
using System;
using Unity.Netcode;

namespace CatGame.Services.SceneManagement
{
    /// <summary>
    /// Estado do carregamento online: qual grupo está sendo carregado e em qual fase.
    /// Grupo e fase andam juntos para chegarem sempre no mesmo pacote.
    /// </summary>
    public struct SceneGroupState : INetworkSerializable, IEquatable<SceneGroupState>
    {
        public static SceneGroupState None => new(Scenes.Bootstrap, SceneGroupPhase.None);

        public readonly Scenes Group => group;
        public readonly SceneGroupPhase Phase => phase;

        private Scenes group;
        private SceneGroupPhase phase;

        public SceneGroupState(Scenes group, SceneGroupPhase phase)
        {
            this.group = group;
            this.phase = phase;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref group);
            serializer.SerializeValue(ref phase);
        }

        public bool Equals(SceneGroupState other)
        {
            return group == other.group && phase == other.phase;
        }
    }
}
