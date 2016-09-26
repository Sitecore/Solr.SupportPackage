using SolrNet.Schema;

using SolrSchemaParser = Sitecore.ContentSearch.SolrProvider.Parsers.SolrSchemaParser;

namespace Sitecore.ContentSearch.SolrProvider.NinjectIntegration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using CommonServiceLocator.NinjectAdapter;

    using HttpWebAdapters;

    using Ninject;
    using Ninject.Integration.SolrNet;
    using Ninject.Integration.SolrNet.Config;

    using Sitecore.Configuration;
    using Sitecore.ContentSearch.SolrProvider.DocumentSerializers;
    using Sitecore.ContentSearch.SolrProvider.SolrNetIntegration;
    using SolrNet;
    using SolrNet.Impl;

    public class NinjectSolrStartUp : ISolrStartUp
    {
        private readonly IKernel kernel;

        private SolrServers Cores;

        public NinjectSolrStartUp(IKernel kernel)
        {
            this.kernel = kernel;

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
                HttpWebRequestFactory = this.kernel.Get<IHttpWebRequestFactory>()
            };

            if (SolrContentSearchManager.EnableHttpCache)
            {
                conn.Cache = this.kernel.Get<ISolrCache>() ?? new NullCache();
            }

            return new SolrCoreAdminEx(conn, this.kernel.Get<ISolrHeaderResponseParser>(), this.kernel.Get<ISolrStatusResponseParser>());
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

            kernel.Load(new SolrNetModule(this.Cores));
            kernel.Bind(typeof(ISolrDocumentResponseParser<Dictionary<string, object>>)).To(typeof(SolrDictionaryDocumentResponseParser));
            kernel.Rebind<ISolrDocumentSerializer<Dictionary<string, object>>>().To<SolrFieldBoostingDictionarySerializer>();
            kernel.Rebind<ISolrSchemaParser>().To<SolrSchemaParser>();
            kernel.Bind<IHttpWebRequestFactory>().ToMethod(x => SolrContentSearchManager.HttpWebRequestFactory);
            kernel.Bind<ISolrCache>().To<HttpRuntimeCache>();

            foreach (SolrServerElement core in this.Cores)
            {
                SolrServerElement closuredCore = core;
                var connection = this.kernel.Get<ISolrConnection>(bm => bm.Get<string>("CoreId") == closuredCore.Id) as SolrConnection;
                if (connection != null)
                {
                    if (SolrContentSearchManager.EnableHttpCache)
                    {
                        connection.Cache = this.kernel.Get<ISolrCache>() ?? new NullCache();
                    }

                    connection.HttpWebRequestFactory = this.kernel.Get<IHttpWebRequestFactory>();
                }
            }

            Microsoft.Practices.ServiceLocation.ServiceLocator.SetLocatorProvider(() => new NinjectServiceLocator(this.kernel));

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