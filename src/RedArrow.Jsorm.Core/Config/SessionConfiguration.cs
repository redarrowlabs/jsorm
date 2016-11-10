﻿using RedArrow.Jsorm.Core.Extensions;
using RedArrow.Jsorm.Core.Map;
using RedArrow.Jsorm.Core.Registry;
using RedArrow.Jsorm.Core.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RedArrow.Jsorm.Core.Config
{
    public class SessionConfiguration
    {
        internal IList<IResourceMap> Maps { get; }
        internal IModelRegistry ModelRegistry { get; set; }

        internal SessionConfiguration()
        {
            Maps = new List<IResourceMap>();
            ModelRegistry = new DefaultModelRegistry();
        }

        public ISessionFactory BuildSessionFactory()
        {
            var factory = new SessionFactory(ModelRegistry);

            foreach (var map in Maps)
            {
                factory.Register(map.ModelType);
                map.Configure(factory);
            }

            return factory;
        }

        public void AddMapsFromAssembly(Assembly assembly)
        {
            assembly.ExportedTypes
                .Where(x => x.GetTypeInfo().IsClass)
                .Where(x => !x.GetTypeInfo().IsAbstract)
                .Where(x => x.GetTypeInfo()
                    .ImplementedInterfaces
                    .Contains(typeof(IResourceMap)))
                .Each(Add);
        }

        public void Add(Type type)
        {
            var ctor = type.GetDefaultConstructor();

            var map = ctor.Invoke(null);

            var item = map as IResourceMap;
            if (item != null)
            {
                Maps.Add(item);
            }
            else
            {
                throw new InvalidOperationException($"Unsupported mapper '{type.FullName}'");
            }
        }
    }
}