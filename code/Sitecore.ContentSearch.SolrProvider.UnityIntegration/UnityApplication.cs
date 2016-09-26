using Sitecore.ContentSearch.SolrProvider.SolrNetIntegration;

namespace Sitecore.ContentSearch.SolrProvider.UnityIntegration
{
    using System;

    using Microsoft.Practices.Unity;

    [Obsolete("Configuration throught application is deprecated. Please add UnityInitializeSolrProvider processor to initialize pipeline instead.")]
    public class UnityApplication : Sitecore.Web.Application
    {
        public IUnityContainer Container { get; set; }

        public virtual void Application_Start()
        {
            if (IntegrationHelper.IsSolrConfigured())
            {
                IntegrationHelper.ReportDoubleSolrConfigurationAttempt(this.GetType());
                return;
            }

            this.Container = new UnityContainer();

            var startup = new UnitySolrStartUp(this.Container);
            startup.Initialize();
        }
    }
}