using Sitecore.ContentSearch.SolrProvider.SolrNetIntegration;

namespace Sitecore.ContentSearch.SolrProvider.StructureMapIntegration
{
    using Sitecore.Pipelines;

    /// <summary>
    /// Initializes Solr provider using StructureMap.
    /// </summary>
    public class StructureMapInitializeSolrProvider
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

            var startup = new StructureMapSolrStartUp();
            startup.Initialize();
        }
    }
}
