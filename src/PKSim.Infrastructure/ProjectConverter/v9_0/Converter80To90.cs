﻿using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using OSPSuite.Core.Domain;
using OSPSuite.Core.Domain.Populations;
using OSPSuite.Core.Extensions;
using OSPSuite.Core.Serialization;
using PKSim.Core;
using PKSim.Infrastructure.Serialization.Xml;
using IoC = OSPSuite.Utility.Container.IContainer;

namespace PKSim.Infrastructure.ProjectConverter.v9_0
{
   public class Converter80To90 : IObjectConverter
   {
      private readonly IoC _container;

      public Converter80To90(IoC container)
      {
         _container = container;
      }

      public bool IsSatisfiedBy(int version) => version == ProjectVersions.V8_0;

      public (int convertedToVersion, bool conversionHappened) Convert(object objectToConvert, int originalVersion)
      {
         return (ProjectVersions.V9_0, false);
      }

      public (int convertedToVersion, bool conversionHappened) ConvertXml(XElement element, int originalVersion)
      {
         var converted = false;
         if (element.Name.IsOneOf("PopulationSimulation", "RandomPopulation", "MoBiPopulation", "ImportedPopulation"))
         {
            convertIndividualValueCache(element);
            converted = true;
         }

         return (ProjectVersions.V9_0, converted);

      }

      private void convertIndividualValueCache(XElement element)
      {
         //With this release, the IndividualPropertiesCache was renamed to IndividualValuesCache
         //Covariates structure was changed completely  (saved by covariate as opposed to saved by individual)
         foreach (var individualPropertiesCacheElement in element.Descendants("IndividualPropertiesCache").ToList())
         {
            var parameterValueCacheElement = individualPropertiesCacheElement.Descendants("ParameterValuesCache");
            var allCovariatesElement = individualPropertiesCacheElement.Descendants("AllCovariates");

            var covariateValuesCacheElement = createCovariateValuesCacheElement(allCovariatesElement);
            convertParameterValueCacheElement(parameterValueCacheElement);

            var individualValuesCacheElement = new XElement("IndividualValuesCache");
            individualValuesCacheElement.Add(parameterValueCacheElement);
            individualValuesCacheElement.Add(covariateValuesCacheElement);
            var parent = individualPropertiesCacheElement.Parent;
            individualPropertiesCacheElement.Remove();
            parent.Add(individualValuesCacheElement);
         }
      }

      private void convertParameterValueCacheElement(IEnumerable<XElement> parameterValueCacheElement)
      {
         foreach (var percentilesListElement in parameterValueCacheElement.Descendants("DoubleList").ToList())
         {
            percentilesListElement.Name = Constants.Serialization.PERCENTILES;
         }
      }

      private XElement createCovariateValuesCacheElement(IEnumerable<XElement> allCovariatesElement)
      {
         var covariateValuesCache = new CovariateValuesCache();
         var individualCovariatesReader = _container.Resolve<IXmlReader<IndividualCovariates>>();
         var covariateValuesCacheWriter = _container.Resolve<IXmlWriter<CovariateValuesCache>>();

         var context = SerializationTransaction.Create();

         // List of old covariates as defined in PKSim 8.x and below
         var allIndividualCovariates = new List<IndividualCovariates>();
         foreach (var individualCovariatesElement in allCovariatesElement.Descendants("IndividualCovariates"))
         {
            var individualCovariates = individualCovariatesReader.ReadFrom(individualCovariatesElement, context);
            allIndividualCovariates.Add(individualCovariates);
         }

         var allCovariateNames = allIndividualCovariates.FirstOrDefault()?.Attributes.Keys.ToList() ?? new List<string>();
         allCovariateNames.Add(Constants.Population.GENDER);
         allCovariateNames.Add(Constants.Population.POPULATION);

         // Transform the old covariates in the new structure
         foreach (var covariateName in allCovariateNames)
         {
            var covariateValues = new CovariateValues(covariateName);
            if (string.Equals(covariateName, Constants.Population.GENDER))
               covariateValues.Values = allIndividualCovariates.Select(x => x.Gender.Name).ToList();
            else if (string.Equals(covariateName, Constants.Population.POPULATION))
               covariateValues.Values = allIndividualCovariates.Select(x => x.Race.Name).ToList();
            else
               covariateValues.Values = allIndividualCovariates.Select(x => x.Covariate(covariateName)).ToList();

            covariateValuesCache.Add(covariateValues);
         }

         return  covariateValuesCacheWriter.WriteFor(covariateValuesCache, context);
      }
   }
}