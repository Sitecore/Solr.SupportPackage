// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="StructureMapSolrStartUp.cs" company="Sitecore">
// //   Copyright (c) Sitecore. All rights reserved.
// // </copyright>
// // --------------------------------------------------------------------------------------------------------------------

using SolrNet.Schema;

namespace Sitecore.ContentSearch.SolrProvider.StructureMapIntegration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using HttpWebAdapters;

    using Sitecore.Configuration;
    using Sitecore.ContentSearch.SolrProvider.DocumentSerializers;
    using Sitecore.ContentSearch.SolrProvider.SolrNetIntegration;
    using SolrNet;
    using SolrNet.Impl;

    using StructureMap;
    using StructureMap.SolrNetIntegration;
    using StructureMap.SolrNetIntegration.Config;

    public class StructureMapSolrStartUp : ISolrStartUp
    {
        internal readonly SolrServers Cores;

        public StructureMapSolrStartUp()
        {
            if (!SolrContentSearchManager.IsEnabled)
            {
                return;
            }

            this.Cores = new SolrServers();
        }

        private ISolrCoreAdminEx BuildCoreAdmin()
        {
            var conn = new SolrConnection(SolrContentSearchManager.ServiceAddress)
            {
                Timeout = SolrContentSearchManager.ConnectionTimeout,
                HttpWebRequestFactory = ObjectFactory.GetInstance<IHttpWebRequestFactory>()
            };

            if (SolrContentSearchManager.EnableHttpCache)
            {
                conn.Cache = ObjectFactory.GetInstance<ISolrCache>() ?? new NullCache();
            }

            return new SolrCoreAdminEx(conn, ObjectFactory.GetInstance<ISolrHeaderResponseParser>(), ObjectFactory.GetInstance<ISolrStatusResponseParser>());
        }

        public void AddCore(string coreId, Type documentType, string coreUrl)
        {
            this.Cores.Add(new SolrServerElement
            {
                Id = coreId,
                DocumentType = documentType.AssemblyQualifiedName,
                Url = coreUrl
            });
        }

        public void Initialize()
        {
            if (!SolrContentSearchManager.IsEnabled)
            {
                throw new InvalidOperationException("Solr configuration is not enabled. Please check your settings and include files.");
            }

            foreach (var index in SolrContentSearchManager.Cores)
            {
                this.AddCore(index, typeof(Dictionary<string, object>), string.Concat(SolrContentSearchManager.ServiceAddress, "/", index));
            }

            ObjectFactory.Initialize(c => c.IncludeRegistry(new SolrNetRegistry(this.Cores)));
            ObjectFactory.Configure(c => c.For(typeof(ISolrDocumentSerializer<Dictionary<string, object>>)).Use(typeof(SolrFieldBoostingDictionarySerializer)));
            ObjectFactory.Configure(c => c.For<ISolrSchemaParser>().Use<Sitecore.ContentSearch.SolrProvider.Parsers.SolrSchemaParser>());
            ObjectFactory.Configure(c => c.For<ISolrCache>().Use<HttpRuntimeCache>());
            ObjectFactory.Configure(c => c.For<IHttpWebRequestFactory>().Use(context => SolrContentSearchManager.HttpWebRequestFactory));

            foreach (var connection in ObjectFactory.GetAllInstances<ISolrConnection>().OfType<SolrConnection>())
            {
                if (SolrContentSearchManager.EnableHttpCache)
                {
                    connection.Cache = ObjectFactory.GetInstance<ISolrCache>() ?? new NullCache();
                }

                connection.HttpWebRequestFactory = ObjectFactory.GetInstance<IHttpWebRequestFactory>();
            }

            Microsoft.Practices.ServiceLocation.ServiceLocator.SetLocatorProvider(() => new StructureMapServiceLocator(ObjectFactory.Container));

            SolrContentSearchManager.SolrAdmin = this.BuildCoreAdmin();
            SolrContentSearchManager.Initialize();
        }

        public bool IsSetupValid()
        {
            if (!SolrContentSearchManager.IsEnabled)
            {
                return false;
            }

            var admin = this.BuildCoreAdmin();
            return SolrContentSearchManager.Cores.Select(defaultIndex => admin.Status(defaultIndex).First()).All(status => status.Name != null);
        }
    }
}