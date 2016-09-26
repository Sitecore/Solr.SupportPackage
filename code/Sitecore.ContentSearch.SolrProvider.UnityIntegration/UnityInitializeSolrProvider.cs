using Sitecore.ContentSearch.SolrProvider.SolrNetIntegration;

namespace Sitecore.ContentSearch.SolrProvider.UnityIntegration
{
    using Microsoft.Practices.Unity;

    using Sitecore.Pipelines;

    /// <summary>
    /// Initializes Solr provider using Unity.
    /// </summary>
    public class UnityInitializeSolrProvider
    {
        public IUnityContainer Container { get; set; }

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

            this.Container = new UnityContainer();

            var startup = new UnitySolrStartUp(this.Container);
            startup.Initialize();
        }
    }
}
