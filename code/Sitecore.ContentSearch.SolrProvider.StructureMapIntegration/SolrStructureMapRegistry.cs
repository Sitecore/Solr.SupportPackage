// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="$filename$" company="Sitecore">
// //   Copyright (c) Sitecore. All rights reserved.
// // </copyright>
// // --------------------------------------------------------------------------------------------------------------------
namespace Sitecore.ContentSearch.SolrProvider.StructureMapIntegration
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;

    using Sitecore.ContentSearch.SolrProvider.DocumentSerializers;

    using SolrNet;
    using SolrNet.Impl;
    using SolrNet.Impl.DocumentPropertyVisitors;
    using SolrNet.Impl.FacetQuerySerializers;
    using SolrNet.Impl.FieldParsers;
    using SolrNet.Impl.FieldSerializers;
    using SolrNet.Impl.QuerySerializers;
    using SolrNet.Impl.ResponseParsers;
    using SolrNet.Mapping;
    using SolrNet.Mapping.Validation;
    using SolrNet.Mapping.Validation.Rules;
    using SolrNet.Schema;
    using SolrNet.Utils;

    using StructureMap.SolrNetIntegration.Config;

    public class SolrNetRegistry : StructureMap.Configuration.DSL.Registry
    {
        public SolrNetRegistry(SolrServers solrServers)
        {
            this.For<IReadOnlyMappingManager>().Use<MemoizingMappingManager>()
                .Ctor<IReadOnlyMappingManager>("mapper").Is(new AttributesMappingManager());
            this.For(typeof(ISolrDocumentActivator<>)).Use(typeof(SolrDocumentActivator<>));
            this.For(typeof(ISolrQueryExecuter<>)).Use(typeof(SolrQueryExecuter<>));
            this.For<ISolrDocumentPropertyVisitor>().Use<DefaultDocumentVisitor>();
            this.For<IMappingValidator>().Use<MappingValidator>();
            this.For<ISolrCache>().Use<NullCache>();

            this.RegisterParsers();
            this.RegisterValidationRules();
            this.RegisterSerializers();
            this.RegisterOperations();

            this.AddCoresFromConfig(solrServers);
        }

        private void RegisterValidationRules()
        {
            var validationRules = new[] {
                                            typeof(MappedPropertiesIsInSolrSchemaRule),
                                            typeof(RequiredFieldsAreMappedRule),
                                            typeof(UniqueKeyMatchesMappingRule),
                                            typeof(MultivaluedMappedToCollectionRule),
                                        };
            foreach (var validationRule in validationRules)
                this.For(typeof(IValidationRule)).Use(validationRule);
        }

        private void RegisterSerializers()
        {
            this.For(typeof(ISolrDocumentSerializer<>)).Use(typeof(SolrDocumentSerializer<>));
            this.For(typeof(ISolrDocumentSerializer<Dictionary<string, object>>)).Use(typeof(SolrFieldBoostingDictionarySerializer));
            this.For<ISolrFieldSerializer>().Use<DefaultFieldSerializer>();
            this.For<ISolrQuerySerializer>().Use<DefaultQuerySerializer>();
            this.For<ISolrFacetQuerySerializer>().Use<DefaultFacetQuerySerializer>();
        }

        private void RegisterOperations()
        {
            this.For(typeof(ISolrBasicReadOnlyOperations<>)).Use(typeof(SolrBasicServer<>));
            this.For(typeof(ISolrBasicOperations<>)).Use(typeof(SolrBasicServer<>));
            this.For(typeof(ISolrReadOnlyOperations<>)).Use(typeof(SolrServer<>));
            this.For(typeof(ISolrOperations<>)).Use(typeof(SolrServer<>));
        }

        private void RegisterParsers()
        {
            this.For(typeof(ISolrDocumentResponseParser<>)).Use(typeof(SolrDocumentResponseParser<>));

            this.For<ISolrDocumentResponseParser<Dictionary<string, object>>>()
                .Use<SolrDictionaryDocumentResponseParser>();

            this.For(typeof(ISolrAbstractResponseParser<>)).Use(typeof(DefaultResponseParser<>));

            this.For<ISolrHeaderResponseParser>().Use<HeaderResponseParser<string>>();
            this.For<ISolrExtractResponseParser>().Use<ExtractResponseParser>();
            this.For(typeof(ISolrMoreLikeThisHandlerQueryResultsParser<>)).Use(typeof(SolrMoreLikeThisHandlerQueryResultsParser<>));
            this.For<ISolrFieldParser>().Use<DefaultFieldParser>();
            this.For<ISolrSchemaParser>().Use<SolrSchemaParser>();
            this.For<ISolrDIHStatusParser>().Use<SolrDIHStatusParser>();
            this.For<ISolrStatusResponseParser>().Use<SolrStatusResponseParser>();
            this.For<ISolrCoreAdmin>().Use<SolrCoreAdmin>();
        }

        /// <summary>
        /// Registers a new core in the container.
        /// This method is meant to be used after the facility initialization
        /// </summary>
        /// <param name="core"></param>
        private void RegisterCore(SolrCore core)
        {
            var coreConnectionId = core.Id + typeof(SolrConnection);

            this.For<ISolrConnection>().Add<SolrConnection>()
                .Named(coreConnectionId)
                .Ctor<string>("serverURL").Is(core.Url)
                .Setter(c => c.Cache).IsTheDefault();

            var ISolrQueryExecuter = typeof(ISolrQueryExecuter<>).MakeGenericType(core.DocumentType);
            var SolrQueryExecuter = typeof(SolrQueryExecuter<>).MakeGenericType(core.DocumentType);

            this.For(ISolrQueryExecuter).Add(SolrQueryExecuter).Named(core.Id + SolrQueryExecuter)
                .CtorDependency<ISolrConnection>("connection").IsNamedInstance(coreConnectionId);

            var ISolrBasicOperations = typeof(ISolrBasicOperations<>).MakeGenericType(core.DocumentType);
            var ISolrBasicReadOnlyOperations = typeof(ISolrBasicReadOnlyOperations<>).MakeGenericType(core.DocumentType);
            var SolrBasicServer = typeof(SolrBasicServer<>).MakeGenericType(core.DocumentType);

            this.For(ISolrBasicOperations).Add(SolrBasicServer).Named(core.Id + SolrBasicServer)
                .CtorDependency<ISolrConnection>("connection").IsNamedInstance(coreConnectionId)
                .Child("queryExecuter").IsNamedInstance(core.Id + SolrQueryExecuter);

            this.For(ISolrBasicReadOnlyOperations).Add(SolrBasicServer).Named(core.Id + SolrBasicServer)
                .CtorDependency<ISolrConnection>("connection").IsNamedInstance(coreConnectionId)
                .Child("queryExecuter").IsNamedInstance(core.Id + SolrQueryExecuter);

            var ISolrOperations = typeof(ISolrOperations<>).MakeGenericType(core.DocumentType);
            var SolrServer = typeof(SolrServer<>).MakeGenericType(core.DocumentType);
            this.For(ISolrOperations).Add(SolrServer).Named(core.Id)
                .Child("basicServer").IsNamedInstance(core.Id + SolrBasicServer);
        }

        private void AddCoresFromConfig(SolrServers solrServers)
        {
            if (solrServers == null)
                return;

            var cores = new List<SolrCore>();

            foreach (SolrServerElement server in solrServers)
            {
                var solrCore = GetCoreFrom(server);
                cores.Add(solrCore);
            }

            foreach (var core in cores)
            {
                this.RegisterCore(core);
            }
        }

        private static SolrCore GetCoreFrom(SolrServerElement server)
        {
            var id = server.Id ?? Guid.NewGuid().ToString();
            var documentType = GetCoreDocumentType(server);
            var coreUrl = GetCoreUrl(server);
            UriValidator.ValidateHTTP(coreUrl);
            return new SolrCore(id, documentType, coreUrl);
        }

        private static string GetCoreUrl(SolrServerElement server)
        {
            var url = server.Url;
            if (string.IsNullOrEmpty(url))
                throw new ConfigurationErrorsException("Core url missing in SolrNet core configuration");
            return url;
        }

        private static Type GetCoreDocumentType(SolrServerElement server)
        {
            var documentType = server.DocumentType;

            if (string.IsNullOrEmpty(documentType))
                throw new ConfigurationErrorsException("Document type missing in SolrNet core configuration");

            Type type;

            try
            {
                type = Type.GetType(documentType);
            }
            catch (Exception e)
            {
                throw new ConfigurationErrorsException(string.Format("Error getting document type '{0}'", documentType), e);
            }

            if (type == null)
                throw new ConfigurationErrorsException(string.Format("Error getting document type '{0}'", documentType));

            return type;
        }
    }

    internal class SolrCore
    {
        public string Id { get; private set; }
        public Type DocumentType { get; private set; }
        public string Url { get; private set; }

        public SolrCore(string id, Type documentType, string url)
        {
            this.Id = id;
            this.DocumentType = documentType;
            this.Url = url;
        }
    }
}