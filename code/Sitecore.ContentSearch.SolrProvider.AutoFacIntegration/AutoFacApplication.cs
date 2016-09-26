using Sitecore.ContentSearch.SolrProvider.SolrNetIntegration;

namespace Sitecore.ContentSearch.SolrProvider.AutoFacIntegration
{
    using System;

    using Autofac;
    [Obsolete("Configuration throught application is deprecated. Please add AutoFacInitializeSolrProvider processor to initialize pipeline instead.")]
    public class AutoFacApplication : Sitecore.Web.Application
    {
        public virtual void Application_Start()
        {
            if (IntegrationHelper.IsSolrConfigured())
            {
                IntegrationHelper.ReportDoubleSolrConfigurationAttempt(this.GetType());
                return;
            }

            var builder = new ContainerBuilder();
            var startup = new AutoFacSolrStartUp(builder);
            startup.Initialize();
        }
    }
}