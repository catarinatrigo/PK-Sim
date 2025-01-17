﻿using System;
using System.Linq;
using System.Xml.Linq;
using PKSim.Core;
using PKSim.Infrastructure.ProjectConverter.v5_2;
using OSPSuite.Core.Converter.v5_4;
using OSPSuite.Core.Domain;
using OSPSuite.Core.Domain.Data;

namespace PKSim.Infrastructure.ProjectConverter.v6_2
{
   public interface IObservedDataConvertor
   {
      void Convert(IProject project, DataRepository observedData, int originalVersion);
      void ConvertDimensionIn(XElement observedDataElement);
   }

   /// <summary>
   ///    Aggregates all changes made in observed data conversion since version 5.1 so that they can be apply in version 6.2
   ///    after db structure change
   /// </summary>
   public class ObservedDataConvertor : IObservedDataConvertor
   {
      private readonly IFormulaAndDimensionConverter _formulaAndDimensionConverter;
      private readonly IDataRepositoryConverter _dataRepositoryConverter;

      public ObservedDataConvertor(IFormulaAndDimensionConverter formulaAndDimensionConverter, IDataRepositoryConverter dataRepositoryConverter)
      {
         _formulaAndDimensionConverter = formulaAndDimensionConverter;
         _dataRepositoryConverter = dataRepositoryConverter;
      }

      public void Convert(IProject project, DataRepository observedData, int originalVersion)
      {
         perform521Conversion(observedData, originalVersion);
         perform541Conversion(project,observedData, originalVersion);
         perform602Conversion(observedData, originalVersion);
      }

      public void ConvertDimensionIn(XElement observedDataElement)
      {
         //retrieve all elements with an attribute dimension
         var allDimensionAttributes = from child in observedDataElement.DescendantsAndSelf()
                                      where child.HasAttributes
                                      let attr = child.Attribute(Constants.Serialization.Attribute.Dimension) ?? child.Attribute("dimension")
                                      where attr != null
                                      select attr;

         foreach (var dimensionAttribute in allDimensionAttributes)
         {
            if (string.Equals(dimensionAttribute.Value, "Concentration"))
               dimensionAttribute.SetValue(CoreConstants.Dimension.MASS_CONCENTRATION);
         }
      }

      private void perform521Conversion(DataRepository observedData, int originalVersion)
      {
         performConversion(originalVersion, ProjectVersions.V5_2_1, () => _formulaAndDimensionConverter.ConvertDimensionIn(observedData));
      }

      private void perform541Conversion(IProject project, DataRepository observedData, int originalVersion)
      {
         performConversion(originalVersion, ProjectVersions.V5_4_1, () =>
         {
            _dataRepositoryConverter.Convert(observedData);
            project.AddClassifiable(new ClassifiableObservedData { Subject = observedData });
         });
      }

      private void perform602Conversion(DataRepository observedData, int originalVersion)
      {
         performConversion(originalVersion, ProjectVersions.V6_0_2, () =>
         {
            var baseGrid = observedData.BaseGrid;
            var baseGridName = baseGrid.Name.Replace(ObjectPath.PATH_DELIMITER, "\\");
            baseGrid.QuantityInfo = new QuantityInfo(baseGrid.Name, new[] {observedData.Name, baseGridName}, QuantityType.Time);
         });
      }

      private void performConversion(int originalVersion, int versionOfConverter, Action action)
      {
         if (originalVersion >= versionOfConverter)
            return;

         action();
      }
   }
}