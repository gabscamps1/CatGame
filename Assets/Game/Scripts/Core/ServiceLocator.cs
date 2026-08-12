using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatGame.Core
{
    public static class ServiceLocator
    {
        private readonly static Dictionary<Type, object> services = new();

        public static void Register<T>(T service)
        {
            services[typeof(T)] = service;
        }

        public static void Unregister<T>()
        {
            services[typeof(T)] = null;
        }

        public static T Get<T>()
        {
            try
            {
                return (T)services[typeof(T)];
            }
            catch
            {
                Debug.LogError($"Services não encontrado. Adicionar o {typeof(T).Name} ao dicionário");
                return default;
            }
        }
    }
}