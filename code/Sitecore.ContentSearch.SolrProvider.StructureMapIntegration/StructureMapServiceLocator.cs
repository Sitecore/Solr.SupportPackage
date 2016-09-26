// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="StructureMapServiceLocator.cs" company="Sitecore">
// //   Copyright (c) Sitecore. All rights reserved.
// // </copyright>
// // --------------------------------------------------------------------------------------------------------------------
namespace Sitecore.ContentSearch.SolrProvider.StructureMapIntegration
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Practices.ServiceLocation;

    using StructureMap;

    public class StructureMapServiceLocator : ServiceLocatorImplBase
    {
        private readonly IContainer _container;

        public StructureMapServiceLocator(IContainer container)
        {
            this._container = container;
        }

        /// <summary>
        ///             When implemented by inheriting classes, this method will do the actual work of resolving
        ///             the requested service instance.
        /// </summary>
        /// <param name="serviceType">Type of instance requested.</param>
        /// <param name="key">Name of registered service you want. May be null.</param>
        /// <returns>
        /// The requested service instance.
        /// </returns>
        protected override object DoGetInstance(Type serviceType, string key)
        {
            if (key != null)
            {
                if (key.Length == 0)
                {
                    throw new ActivationException();
                }

                return this._container.GetInstance(serviceType, key);
            }


            return this._container.GetInstance(serviceType);
        }

        /// <summary>
        ///             When implemented by inheriting classes, this method will do the actual work of
        ///             resolving all the requested service instances.
        /// </summary>
        /// <param name="serviceType">Type of service requested.</param>
        /// <returns>
        /// Sequence of service instance objects.
        /// </returns>
        protected override IEnumerable<object> DoGetAllInstances(Type serviceType)
        {
            foreach (object obj in this._container.GetAllInstances(serviceType))
            {
                yield return obj;
            }
        }
    }
}