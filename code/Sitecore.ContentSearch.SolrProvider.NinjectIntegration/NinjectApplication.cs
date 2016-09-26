using Sitecore.ContentSearch.SolrProvider.SolrNetIntegration;

namespace Sitecore.ContentSearch.SolrProvider.NinjectIntegration
{
    using System;

    using Ninject;

    [Obsolete("Configuration throught application is deprecated. Please add NinjectInitializeSolrProvider processor to initialize pipeline instead.")]
    public class NinjectApplication : Sitecore.Web.Application
    {
        public IKernel Container { get; set; }

        public virtual void Application_Start()
        {
            if (IntegrationHelper.IsSolrConfigured())
            {
                IntegrationHelper.ReportDoubleSolrConfigurationAttempt(this.GetType());
                return;
            }

            this.Container = new StandardKernel();

            var startup = new NinjectSolrStartUp(this.Container);
            startup.Initialize();
        }
    }
}