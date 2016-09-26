using Sitecore.ContentSearch.SolrProvider.SolrNetIntegration;

namespace Sitecore.ContentSearch.SolrProvider.StructureMapIntegration
{
    using System;

    [Obsolete("Configuration throught application is deprecated. Please add StructureMapInitializeSolrProvider processor to initialize pipeline instead.")]
    public class StructureMapApplication : Sitecore.Web.Application
    {
        public virtual void Application_Start()
        {
            if (IntegrationHelper.IsSolrConfigured())
            {
                IntegrationHelper.ReportDoubleSolrConfigurationAttempt(this.GetType());
                return;
            }

            var startup = new StructureMapSolrStartUp();
            startup.Initialize();
        }
    }
}