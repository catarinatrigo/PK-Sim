using System.Collections.Generic;
using OSPSuite.Utility.Collections;
using OSPSuite.Core.Domain;

namespace PKSim.Core.Model
{
   public class Species : ObjectBase
   {
      private readonly IList<SpeciesPopulation> _allPopulations = new List<SpeciesPopulation>();
      private readonly ICache<string, ParameterValueVersionCategory> _pvvCategories = new Cache<string, ParameterValueVersionCategory>(pvv => pvv.Name);
      public virtual string DisplayName { get; set; }

      public virtual SpeciesPopulation PopulationByName(string name)
      {
         return _allPopulations.FindByName(name);
      }

      public virtual IEnumerable<SpeciesPopulation> Populations
      {
         get { return _allPopulations; }
      }

      public virtual void AddPopulation(SpeciesPopulation speciesPopulation)
      {
         _allPopulations.Add(speciesPopulation);
      }

      public virtual IEnumerable<ParameterValueVersionCategory> PVVCategories
      {
         get { return _pvvCategories; }
      }

      public virtual void AddPVVCategory(ParameterValueVersionCategory pvvCategory)
      {
         _pvvCategories.Add(pvvCategory);
      }

      public virtual ParameterValueVersionCategory PVVCategoryByName(string categoryName)
      {
         return _pvvCategories[categoryName];
      }

      public virtual bool IsHuman
      {
         get { return string.Equals(Name, CoreConstants.Species.Human); }
      }
   }
}