using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Ssit.CrossX2.Editor.Helpers;

namespace Ssit.CrossX2.Editor.Service
{
    public interface IServices
    {
        TInterface Register<TInterface>([NotNull] TInterface service, bool overwrite = false) where TInterface : class;
        TInterface Get<TInterface>() where TInterface : class;
        TInterface GetOptional<TInterface>() where TInterface : class;

        TImplementation Register<TInterface, TImplementation>() where TInterface : class where TImplementation : TInterface;

        bool Resolve(Type type, out object instance);
    
        TObject Create<TObject>(object parameter = null);
    }

    public class Services: IServices
    {
        private readonly IServices _parent;
        private readonly ConcurrentDictionary<Type, object> _services = new();

        public Services()
        {
            Register<IServices>(this);
        }

        public Services(IServices parent)
        {
            _parent = parent;
        }
    
        public TInterface Register<TInterface>([NotNull] TInterface service, bool overwrite = false) where TInterface: class
        {
            if (service is null) throw new NullReferenceException();

            if (overwrite)
            {
                _services.TryRemove(typeof(TInterface), out _);
            }

            _services.TryAdd(typeof(TInterface), service);
            return service;
        }

        public TInterface Get<TInterface>() where TInterface: class
        {
            if (Resolve(typeof(TInterface), out var instance))
            {
                return (TInterface)instance;
            }

            throw new KeyNotFoundException();
        }
    
        public TInterface GetOptional<TInterface>() where TInterface: class
        {
            if (Resolve(typeof(TInterface), out var instance))
            {
                return (TInterface)instance;
            }
        
            return default;
        }

        public TImplementation Register<TInterface, TImplementation>() where TInterface : class where TImplementation : TInterface
        {
            var imp = Create<TImplementation>();
            Register<TInterface>(imp);
            return imp;
        }

        public TObject Create<TObject>(object parameter = null)
        {
            return (TObject)ObjectCreationHelper.CreateObject(typeof(TObject), parameter, Resolve);
        }

        public bool Resolve(Type type, out object instance)
        {
            if (_services.TryGetValue(type, out instance))
            {
                return true;
            }

            if (_parent != null && _parent.Resolve(type, out instance))
            {
                return true;
            }
        
            return false;
        }
    }
}