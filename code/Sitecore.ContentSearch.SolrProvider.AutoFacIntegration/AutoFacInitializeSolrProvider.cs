using Sitecore.ContentSearch.SolrProvider.SolrNetIntegration;

namespace Sitecore.ContentSearch.SolrProvider.AutoFacIntegration
{
    using Autofac;

    using Sitecore.Pipelines;

    /// <summary>
    /// Initializes Solr provider using AutoFac.
    /// </summary>
    public class AutoFacInitializeSolrProvider
    {
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

            var builder = new ContainerBuilder();
            var startup = new AutoFacSolrStartUp(builder);
            startup.Initialize();
        }
    }
}
