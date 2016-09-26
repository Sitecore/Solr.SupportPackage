using Sitecore.ContentSearch.SolrProvider.SolrNetIntegration;

namespace Sitecore.ContentSearch.SolrProvider.CastleWindsorIntegration
{
    using Castle.Windsor;

    using Sitecore.Pipelines;

    /// <summary>
    /// Initializes Solr provider using Windsor.
    /// </summary>
    public class WindsorInitializeSolrProvider
    {
        public IWindsorContainer Container { get; set; }

        /// <summary>
        /// Performs Solr provider initialization.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public void Process(PipelineArgs args)
        {
            if (!SolrContentSearchManager.IsEnabled)
            {
                return;
            }

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
