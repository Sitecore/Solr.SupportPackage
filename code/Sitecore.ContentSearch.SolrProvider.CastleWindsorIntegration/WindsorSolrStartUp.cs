// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="$filename$" company="Sitecore">
// //   Copyright (c) Sitecore. All rights reserved.
// // </copyright>
// // --------------------------------------------------------------------------------------------------------------------

using SolrNet.Impl;
using SolrNet.Schema;

namespace Sitecore.ContentSearch.SolrProvider.CastleWindsorIntegration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Castle.Facilities.SolrNetIntegration;
    using Castle.MicroKernel.Registration;
    using Castle.Windsor;

    using HttpWebAdapters;

    using Sitecore.Configuration;
    using Sitecore.ContentSearch.SolrProvider.DocumentSerializers;
    using Sitecore.ContentSearch.SolrProvider.SolrNetIntegration;
    using SolrNet;
    using SolrNet.Mapping.Validation;

    /// <summary>
    /// Creates the facility and sets up the service locator, also provides configuration validation.
    /// </summary>
    public class WindsorSolrStartUp : ISolrStartUp
    {
        internal readonly IWindsorContainer Container;

        internal SolrNetFacility SolrFacility;

        public WindsorSolrStartUp(IWindsorContainer container)
        {
            if (!SolrContentSearchManager.IsEnabled)
            {
                return;
            }

            this.Container = container;  
            this.SolrFacility = new SolrNetFacility(SolrContentSearchManager.ServiceAddress);
        }

        public void AddCore(string coreId, Type documentType, string coreUrl)
        {
            this.SolrFacility.AddCore(coreId, documentType, coreUrl);
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

            this.Container.AddFacility(this.SolrFacility);

            // Override default implementation registration with custom object.
            this.Container.Register(Component.For<ISolrDocumentSerializer<Dictionary<string, object>>>().ImplementedBy<SolrFieldBoostingDictionarySerializer>().OverridesExistingRegistration());
            this.Container.Register(Component.For<ISolrSchemaParser>().ImplementedBy<Sitecore.ContentSearch.SolrProvider.Parsers.SolrSchemaParser>().OverridesExistingRegistration());
            this.Container.Register(Component.For<ISolrCache>().ImplementedBy<HttpRuntimeCache>());
            this.Container.Register(Component.For<IHttpWebRequestFactory>().UsingFactoryMethod(() => SolrContentSearchManager.HttpWebRequestFactory));

            foreach (var connection in this.Container.ResolveAll<ISolrConnection>().OfType<SolrConnection>())
            {
                if (SolrContentSearchManager.EnableHttpCache)
                {
                    connection.Cache = this.Container.Resolve<ISolrCache>() ?? new NullCache();
                }

                connection.HttpWebRequestFactory = this.Container.Resolve<IHttpWebRequestFactory>();
            }

            Microsoft.Practices.ServiceLocation.ServiceLocator.SetLocatorProvider(() => new WindsorServiceLocator(this.Container));

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

        /// <summary>
        /// Builds the core admin.
        /// </summary>
        /// <returns>The core admin object.</returns>
        private ISolrCoreAdminEx BuildCoreAdmin()
        {
            var connection = new SolrConnection(SolrContentSearchManager.ServiceAddress)
            {
                Timeout = SolrContentSearchManager.ConnectionTimeout,
                HttpWebRequestFactory = this.Container.Resolve<IHttpWebRequestFactory>()
            };

            if (SolrContentSearchManager.EnableHttpCache)
            {
                connection.Cache = this.Container.Resolve<ISolrCache>() ?? new NullCache();
            }

            return new SolrCoreAdminEx(connection, this.Container.Resolve<ISolrHeaderResponseParser>(), this.Container.Resolve<ISolrStatusResponseParser>());
        }
    }

    public static class WindsorExtensions
    {
        public static ComponentRegistration<T> OverridesExistingRegistration<T>(this ComponentRegistration<T> componentRegistration) where T : class
        {
            return componentRegistration
                                .Named(Guid.NewGuid().ToString())
                                .IsDefault();
        }
    }
}