using SolrNet.Schema;

using SolrSchemaParser = Sitecore.ContentSearch.SolrProvider.Parsers.SolrSchemaParser;

namespace Sitecore.ContentSearch.SolrProvider.AutoFacIntegration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Autofac;

    using AutofacContrib.CommonServiceLocator;
    using AutofacContrib.SolrNet;
    using AutofacContrib.SolrNet.Config;

    using HttpWebAdapters;

    using Sitecore.Configuration;
    using Sitecore.ContentSearch.SolrProvider.DocumentSerializers;
    using Sitecore.ContentSearch.SolrProvider.SolrNetIntegration;
    using SolrNet;
    using SolrNet.Impl;

    public class AutoFacSolrStartUp : ISolrStartUp
    {
        private readonly ContainerBuilder builder;

        private readonly SolrServers Cores;

        private IContainer container;

        public AutoFacSolrStartUp(ContainerBuilder builder)
        {
            if (!SolrContentSearchManager.IsEnabled)
            {
                return;
            }

            this.builder = builder;
            this.Cores = new SolrServers();
        }

        private ISolrCoreAdminEx BuildCoreAdmin()
        {
            var conn = new SolrConnection(SolrContentSearchManager.ServiceAddress)
            {
                Timeout = SolrContentSearchManager.ConnectionTimeout,
                HttpWebRequestFactory = this.container.Resolve<IHttpWebRequestFactory>()
            };

            if (SolrContentSearchManager.EnableHttpCache)
            {
                conn.Cache = this.container.Resolve<ISolrCache>() ?? new NullCache();
            }
            
            return new SolrCoreAdminEx(conn, this.container.Resolve<ISolrHeaderResponseParser>(), this.container.Resolve<ISolrStatusResponseParser>());
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

            this.builder.RegisterModule(new SolrNetModule(this.Cores));
            this.builder.RegisterType<SolrFieldBoostingDictionarySerializer>().As<ISolrDocumentSerializer<Dictionary<string, object>>>();
            this.builder.RegisterType<SolrSchemaParser>().As<ISolrSchemaParser>();

            this.builder.Register(c => SolrContentSearchManager.HttpWebRequestFactory).As<IHttpWebRequestFactory>();
            this.builder.RegisterType<HttpRuntimeCache>().As<ISolrCache>();

            foreach (SolrServerElement core in this.Cores)
            {
                string coreConnectionId = core.Id + typeof(SolrConnection);
                var parameters = new[] { new NamedParameter("serverURL", core.Url) };
                var properties = new[] { new NamedParameter("Timeout", SolrContentSearchManager.ConnectionTimeout) };
                this.builder.RegisterType(typeof(SolrConnection))
                    .Named(coreConnectionId, typeof(ISolrConnection))
                    .WithParameters(parameters)
                    .WithProperties(properties)
                    .OnActivated(args => 
                    {
                        if (SolrContentSearchManager.EnableHttpCache)
                        {
                           ((SolrConnection)args.Instance).Cache = args.Context.Resolve<ISolrCache>();
                        }
                        ((SolrConnection)args.Instance).HttpWebRequestFactory = args.Context.Resolve<IHttpWebRequestFactory>();
                });
            }

            this.container = this.builder.Build();

            Microsoft.Practices.ServiceLocation.ServiceLocator.SetLocatorProvider(() => new AutofacServiceLocator(container));

            SolrContentSearchManager.SolrAdmin = this.BuildCoreAdmin();
            SolrContentSearchManager.Initialize();
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