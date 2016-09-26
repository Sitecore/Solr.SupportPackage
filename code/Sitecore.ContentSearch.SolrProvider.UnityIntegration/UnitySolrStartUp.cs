// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UnitySolrStartUp.cs" company="Sitecore">
//   Copyright (c) Sitecore. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using SolrNet.Schema;

namespace Sitecore.ContentSearch.SolrProvider.UnityIntegration
{
  using System;
  using System.Collections.Generic;
  using System.Linq;

  using HttpWebAdapters;

  using Microsoft.Practices.ServiceLocation;
  using Microsoft.Practices.Unity;

  using Sitecore.Configuration;
  using Sitecore.ContentSearch.SolrProvider.DocumentSerializers;
  using Sitecore.ContentSearch.SolrProvider.SolrNetIntegration;
  using Sitecore.Diagnostics;

  using SolrNet;
  using SolrNet.Impl;

  using Unity.SolrNetIntegration;
  using Unity.SolrNetIntegration.Config;

  /// <summary>
  /// The unity solr start up.
  /// </summary>
  public class UnitySolrStartUp : ISolrStartUp
  {
    #region Fields

    /// <summary>
    /// The cores.
    /// </summary>
    internal readonly SolrServers Cores;

    /// <summary>
    /// The container.
    /// </summary>
    internal IUnityContainer Container;

    #endregion

    #region Constructors and Destructors

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitySolrStartUp"/> class.
    /// </summary>
    /// <param name="container">
    /// The container.
    /// </param>
    public UnitySolrStartUp([NotNull] IUnityContainer container)
    {
      Assert.ArgumentNotNull(container, "container");
      if (!SolrContentSearchManager.IsEnabled)
      {
        return;
      }

      this.Container = container;
      this.Cores = new SolrServers();
    }

    #endregion

    #region Public Methods and Operators

    /// <summary>
    /// Adds the core.
    /// </summary>
    /// <param name="coreId">The core id.</param>
    /// <param name="documentType">The document type.</param>
    /// <param name="coreUrl">The core url.</param>
    public void AddCore([NotNull] string coreId, [NotNull] Type documentType, [NotNull] string coreUrl)
    {
      Assert.ArgumentNotNull(coreId, "coreId");
      Assert.ArgumentNotNull(documentType, "documentType");
      Assert.ArgumentNotNull(coreUrl, "coreUrl");
      this.Cores.Add(new SolrServerElement { Id = coreId, DocumentType = documentType.AssemblyQualifiedName, Url = coreUrl });
    }

    /// <summary>
    /// Initializes this instance.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">Solr configuration is not enabled. Please check your settings and include files.</exception>
    /// <exception cref="InvalidOperationException">Solr configuration is not enabled. Please check your settings and include files.</exception>
    public void Initialize()
    {
      if (!SolrContentSearchManager.IsEnabled)
      {
        throw new InvalidOperationException("Solr configuration is not enabled. Please check your settings and include files.");
      }

      foreach (string index in SolrContentSearchManager.Cores)
      {
        this.AddCore(index, typeof(Dictionary<string, object>), string.Concat(SolrContentSearchManager.ServiceAddress, "/", index));
      }

      this.Container = new SolrNetContainerConfiguration().ConfigureContainer(this.Cores, this.Container);
      this.Container.RegisterType(typeof(ISolrDocumentSerializer<Dictionary<string, object>>), typeof(SolrFieldBoostingDictionarySerializer));
      this.Container.RegisterType(typeof(ISolrSchemaParser), typeof(Sitecore.ContentSearch.SolrProvider.Parsers.SolrSchemaParser));
      this.Container.RegisterType(typeof(ISolrCache), typeof(HttpRuntimeCache));
      this.Container.RegisterType<IHttpWebRequestFactory>(new InjectionFactory(c => SolrContentSearchManager.HttpWebRequestFactory));

      List<ContainerRegistration> registrations = this.Container.Registrations.Where(r => r.RegisteredType == typeof(ISolrConnection)).ToList();
      if (registrations.Count > 0)
      {
        foreach (ContainerRegistration registration in registrations)
        {
          SolrServerElement solrCore = this.Cores.FirstOrDefault(core => registration.Name == core.Id + registration.MappedToType.FullName);

          if (solrCore == null)
          {
            Log.Error(
              "The Solr Core configuration for the '"
              + registration.Name
              + "' Unity registration could not be found. The HTTP cache and HTTP web request factory for the Solr connection to the Core cannot be configured.",
              this);
            continue;
          }

          List<InjectionMember> injectionParameters = new List<InjectionMember>()
          {
            new InjectionConstructor(solrCore.Url),
            new InjectionProperty("HttpWebRequestFactory", new ResolvedParameter<IHttpWebRequestFactory>()),
            new InjectionProperty("Timeout", SolrContentSearchManager.ConnectionTimeout)
          };

          if (SolrContentSearchManager.EnableHttpCache)
          {
            injectionParameters.Add(new InjectionProperty("Cache", new ResolvedParameter<ISolrCache>()));
          }

          this.Container.RegisterType(typeof(ISolrConnection), typeof(SolrConnection), registration.Name, null, injectionParameters.ToArray());
        }
      }

      ServiceLocator.SetLocatorProvider(() => new UnityServiceLocator(this.Container));

      SolrContentSearchManager.SolrAdmin = this.BuildCoreAdmin();
      SolrContentSearchManager.Initialize();
    }

    /// <summary>
    /// Determines whether setup is valid.
    /// </summary>
    /// <returns>
    /// The <see cref="bool" />.
    /// </returns>
    public bool IsSetupValid()
    {
      if (!SolrContentSearchManager.IsEnabled)
      {
        return false;
      }

      ISolrCoreAdmin admin = this.BuildCoreAdmin();
      return SolrContentSearchManager.Cores.Select(defaultIndex => admin.Status(defaultIndex).First()).All(status => status.Name != null);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Builds the core admin.
    /// </summary>
    /// <returns>
    /// The <see cref="ISolrCoreAdmin" />.
    /// </returns>
    [NotNull]
    private ISolrCoreAdminEx BuildCoreAdmin()
    {
      var conn = new SolrConnection(SolrContentSearchManager.ServiceAddress)
      {
        Timeout = SolrContentSearchManager.ConnectionTimeout,
        HttpWebRequestFactory = this.Container.Resolve<IHttpWebRequestFactory>()
      };

      if (SolrContentSearchManager.EnableHttpCache)
      {
        conn.Cache = this.Container.Resolve<ISolrCache>() ?? new NullCache();
      }

      return new SolrCoreAdminEx(conn, this.Container.Resolve<ISolrHeaderResponseParser>(), this.Container.Resolve<ISolrStatusResponseParser>());
    }

    #endregion
  }
}