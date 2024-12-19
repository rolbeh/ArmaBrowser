using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ArmaBrowser.Logic
{
    internal sealed class ServiceHub : IServiceProvider
    {
        private readonly IDictionary<Type, IServiceEntry> _dictionary = new Dictionary<Type, IServiceEntry>();

        static ServiceHub()
        {
            Instance = new ServiceHub();
        }

        internal static ServiceHub Instance { get; }

        #region Implementation of IServiceProvider

        public object GetService(Type serviceType)
        {
            lock (_dictionary)
            {
                if (!_dictionary.TryGetValue(serviceType, out var entry)) throw new KeyNotFoundException();

                return entry.GetInstance();
            }
        }

        #endregion

        [DebuggerStepThrough]
        public TService GetService<TService>()
        {
            return (TService) GetService(typeof(TService));
        }

        public TService Set<TService>(TService serviceInstance)
        {
            lock (_dictionary)
            {
                _dictionary[typeof(TService)] = new StaticEntry(serviceInstance);
            }

            return serviceInstance;
        }

        private interface IServiceEntry
        {
            object GetInstance();
        }

        private class StaticEntry : IServiceEntry
        {
            private readonly object _instance;

            public StaticEntry(object instance)
            {
                _instance = instance;
            }

            #region Implementation of IServiceEntry

            public object GetInstance()
            {
                return _instance;
            }

            #endregion
        }
    }
}