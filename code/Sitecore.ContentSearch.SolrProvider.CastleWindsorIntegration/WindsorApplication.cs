using Sitecore.ContentSearch.SolrProvider.SolrNetIntegration;

namespace Sitecore.ContentSearch.SolrProvider.CastleWindsorIntegration
{
    using System;

    using Castle.Windsor;

    [Obsolete("Configuration throught application is deprecated. Please add WindsorInitializeSolrProvider processor to initialize pipeline instead.")]
    public class WindsorApplication : Sitecore.Web.Application
    {
        public IWindsorContainer Container { get; set; }

        public virtual void Application_Start()
        {
            if (IntegrationHelper.IsSolrConfigured())
            {
                IntegrationHelper.ReportDoubleSolrConfigurationAttempt(this.GetType());
                return;
            }

            this.Container = new WindsorContainer();

            var startup = new WindsorSolrStartUp(this.Container);
            startup.Initialize();
        }
    }
}