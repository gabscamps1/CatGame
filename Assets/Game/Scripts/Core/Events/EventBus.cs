using System;
using System.Collections.Generic;
using CatGame.Core.Interfaces;

namespace CatGame.Core.Events
{
    public static class EventBus
    {
        private readonly static Dictionary<Type, Delegate> events = new();

        /// <summary>
        /// Método para inscrever um evento.
        /// </summary>
        public static void Subscribe<T>(Action<T> listener) where T : IGlobalEvent, new()
        {
            if (events.TryGetValue(typeof(T), out var existing))
                events[typeof(T)] = Delegate.Combine(existing, listener);
            else
                events[typeof(T)] = listener;
        }

        /// <summary>
        /// Método para desinscrever um evento.
        /// </summary>
        public static void Unsubscribe<T>(Action<T> listener) where T : IGlobalEvent, new()
        {
            if (!events.TryGetValue(typeof(T), out var existing))
                return;

            var current = Delegate.Remove(existing, listener);

            if (current != null)
                events[typeof(T)] = current;
            else
                events.Remove(typeof(T));
        }

        /// <summary>
        /// Método para disparar um evento.
        /// </summary>
        public static void Publish<T>(T action) where T : IGlobalEvent, new()
        {
            if (events.TryGetValue(typeof(T), out var existing))
            {
                var callback = existing as Action<T>;
                callback?.Invoke(action);
            }
        }
    }
}
