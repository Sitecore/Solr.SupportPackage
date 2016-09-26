using Sitecore.ContentSearch.SolrProvider.SolrNetIntegration;

namespace Sitecore.ContentSearch.SolrProvider.NinjectIntegration
{
    using Ninject;

    using Sitecore.Pipelines;

    /// <summary>
    /// Initializes Solr provider using Ninject.
    /// </summary>
    public class NinjectInitializeSolrProvider
    {
        public IKernel Container { get; set; }

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

            this.Container = new StandardKernel();

            var startup = new NinjectSolrStartUp(this.Container);
            startup.Initialize();
        }
    }
}
