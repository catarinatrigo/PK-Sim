using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using FakeItEasy;
using OSPSuite.BDDHelper;
using OSPSuite.BDDHelper.Extensions;
using OSPSuite.Core.Domain;
using OSPSuite.Core.Domain.Data;
using OSPSuite.Core.Domain.Mappers;
using OSPSuite.Core.Domain.Services;
using OSPSuite.Core.Serialization.SimModel.Services;
using OSPSuite.Core.Services;
using PKSim.Assets;
using PKSim.Core;
using PKSim.Core.Model;
using PKSim.Core.Services;
using PKSim.Infrastructure.Services;
using ILazyLoadTask = PKSim.Core.Services.ILazyLoadTask;

namespace PKSim.Infrastructure
{
   public abstract class concern_for_SimulationExportTask : ContextSpecification<ISimulationExportTask>
   {
      private IQuantityPathToQuantityDisplayPathMapper _quantityDisplayPathMapper;
      protected ILazyLoadTask _lazyLoadTask;
      protected IDialogCreator _dialogCreator;
      protected IDataRepositoryExportTask _dataRepositoryTask;
      private IStringSerializer _stringSerializer;
      private IModelReportCreator _modelReportCreator;
      private ISimulationToModelCoreSimulationMapper _simulationMapper;
      private ISimModelExporter _simModelExporter;
      protected ISimulationResultsToDataTableConverter _simulationResultsToDataTableConverter;

      protected override void Context()
      {
         _quantityDisplayPathMapper = A.Fake<IQuantityPathToQuantityDisplayPathMapper>();
         _lazyLoadTask = A.Fake<ILazyLoadTask>();
         _dialogCreator = A.Fake<IDialogCreator>();
         _dataRepositoryTask = A.Fake<IDataRepositoryExportTask>();
         _stringSerializer = A.Fake<IStringSerializer>();
         _modelReportCreator = A.Fake<IModelReportCreator>();
         _simulationMapper = A.Fake<ISimulationToModelCoreSimulationMapper>();
         _simModelExporter = A.Fake<ISimModelExporter>();
         _simulationResultsToDataTableConverter = A.Fake<ISimulationResultsToDataTableConverter>();
         sut = new SimulationExportTask(_lazyLoadTask, _dialogCreator, _dataRepositoryTask, _quantityDisplayPathMapper,
            _stringSerializer, _modelReportCreator, _simulationMapper, _simModelExporter, _simulationResultsToDataTableConverter);
      }
   }

   public class When_exporting_the_simulation_results_of_a_simulation_without_result_to_excel : concern_for_SimulationExportTask
   {
      private IndividualSimulation _simulation;

      protected override void Context()
      {
         base.Context();
         _simulation = new IndividualSimulation();
      }

      [Observation]
      public void should_throw_an_exception_indicating_that_the_simulation_should_be_calculated_first()
      {
         The.Action(() => sut.ExportResultsToExcelAsync(_simulation)).ShouldThrowAn<PKSimException>();
      }
   }

   public class When_exporting_the_simulation_results_of_a_simulation_having_result_to_excel_and_the_user_cancels_the_file_selection : concern_for_SimulationExportTask
   {
      private IndividualSimulation _simulation;

      protected override void Context()
      {
         Debug.Print(AppDomain.CurrentDomain.BaseDirectory);
         base.Context();
         _simulation = A.Fake<IndividualSimulation>();
         A.CallTo(() => _simulation.HasResults).Returns(true);
         A.CallTo(() => _dialogCreator.AskForFileToSave(PKSimConstants.UI.ExportSimulationResultsToExcel, Constants.Filter.EXCEL_SAVE_FILE_FILTER, Constants.DirectoryKey.REPORT, null, null)).Returns(string.Empty);
      }

      [Observation]
      public async Task should_not_export_the_results_to_excel()
      {
         await sut.ExportResultsToExcelAsync(_simulation);
         A.CallTo(() => _dataRepositoryTask.ExportToExcel(_simulation.DataRepository, A<string>.Ignored, true, A<DataColumnExportOptions>._)).MustNotHaveHappened();
      }
   }

   public class When_exporting_the_simulation_results_of_a_simulation_having_result_to_excel_and_the_user_selects_a_file : concern_for_SimulationExportTask
   {
      private IndividualSimulation _simulation;
      private string _excelFile;
      private IReadOnlyList<DataTable> _dataTables;

      protected override void Context()
      {
         base.Context();
         _simulation = A.Fake<IndividualSimulation>();
         _dataTables = new List<DataTable>();
         A.CallTo(() => _simulation.HasResults).Returns(true);
         A.CallTo(() => _simulation.Name).Returns("toto");
         A.CallTo(() => _simulation.DataRepository).Returns(new DataRepository());
         _excelFile = "tralala";
         A.CallTo(() => _dialogCreator.AskForFileToSave(PKSimConstants.UI.ExportSimulationResultsToExcel, Constants.Filter.EXCEL_SAVE_FILE_FILTER, Constants.DirectoryKey.REPORT, CoreConstants.DefaultResultsExportNameFor(_simulation.Name), null)).Returns(_excelFile);
      }

      [Observation]
      public async Task should_export_the_results_to_excel()
      {
         await sut.ExportResultsToExcelAsync(_simulation);
         A.CallTo(() => _dataRepositoryTask.ExportToExcelAsync(_simulation.DataRepository, _excelFile, true, A<DataColumnExportOptions>._)).MustHaveHappened();
      }
   }

   public class When_exporting_the_simulation_results_to_a_csv_file : concern_for_SimulationExportTask
   {
      private Simulation _simulation;
      private string _fileFullPath;

      protected override void Context()
      {
         base.Context();
         _simulation = A.Fake<Simulation>().WithName("Sim");
         A.CallTo(() => _simulation.HasResults).Returns(true);
         _fileFullPath = "file full path";
         A.CallTo(() => _dialogCreator.AskForFileToSave(PKSimConstants.UI.ExportSimulationResultsToCSV, Constants.Filter.CSV_FILE_FILTER, Constants.DirectoryKey.REPORT, CoreConstants.DefaultResultsExportNameFor(_simulation.Name), null))
            .Returns(_fileFullPath);
      }

      [Observation]
      public async Task should_load_the_results()
      {
         await sut.ExportResultsToCSVAsync(_simulation);
         A.CallTo(() => _lazyLoadTask.LoadResults(_simulation)).MustHaveHappened();
      }

      [Observation]
      public async Task should_export_the_file_to_csv()
      {
         await sut.ExportResultsToCSVAsync(_simulation);
         A.CallTo(() => _simulationResultsToDataTableConverter.ResultsToDataTableAsync(_simulation.Results, _simulation)).MustHaveHappened();
      }
   }

   public class When_exporting_a_simulation_without_results_to_a_csv_file : concern_for_SimulationExportTask
   {
      private Simulation _simulation;
      private bool _exceptionRaised;

      protected override void Context()
      {
         base.Context();
         _simulation = A.Fake<Simulation>().WithName("Sim");
         A.CallTo(() => _simulation.HasResults).Returns(false);
      }

      [Observation]
      public async Task should_throw_an_exception()
      {
         try
         {
            await sut.ExportResultsToCSVAsync(_simulation);
            _exceptionRaised = false;
         }
         catch (Exception)
         {
            _exceptionRaised = true;
         }

         _exceptionRaised.ShouldBeTrue();
      }
   }

   public class When_exporting_the_simulation_results_to_a_csv_file_and_export_is_canceled : concern_for_SimulationExportTask
   {
      private Simulation _simulation;

      protected override void Context()
      {
         base.Context();
         _simulation = A.Fake<Simulation>().WithName("Sim");
         A.CallTo(() => _simulation.HasResults).Returns(true);
         A.CallTo(_dialogCreator).WithReturnType<string>().Returns(string.Empty);
      }

      [Observation]
      public async Task should_load_the_results()
      {
         await sut.ExportResultsToCSVAsync(_simulation);
         A.CallTo(() => _lazyLoadTask.LoadResults(_simulation)).MustHaveHappened();
      }

      [Observation]
      public async Task should_not_export_the_file_to_csv()
      {
         await sut.ExportResultsToCSVAsync(_simulation);
         A.CallTo(() => _simulationResultsToDataTableConverter.ResultsToDataTableAsync(_simulation.Results, _simulation)).MustNotHaveHappened();
      }
   }

   public class When_exporting_the_pk_analysis_for_a_simulation_that_does_not_have_any_available : concern_for_SimulationExportTask
   {
      private PopulationSimulation _populationSimulation;
      private bool _exceptionRaised;

      protected override void Context()
      {
         base.Context();
         _populationSimulation = A.Fake<PopulationSimulation>();
         A.CallTo(() => _populationSimulation.HasPKAnalyses).Returns(false);
      }

      [Observation]
      public async Task should_throw_an_exception()
      {
         try
         {
            await sut.ExportPKAnalysesToCSVAsync(_populationSimulation);
            _exceptionRaised = false;
         }
         catch (Exception)
         {
            _exceptionRaised = true;
         }

         _exceptionRaised.ShouldBeTrue();
      }
   }

   public class When_exporting_the_simulation_pk_analysis_to_an_excel_file : concern_for_SimulationExportTask
   {
      private PopulationSimulation _populationSimulation;
      private string _fileFullPath;
      private DataTable _dataTable;

      protected override void Context()
      {
         base.Context();
         _dataTable = new DataTable();
         _populationSimulation = A.Fake<PopulationSimulation>().WithName("POP");
         A.CallTo(() => _populationSimulation.HasPKAnalyses).Returns(true);
         _fileFullPath = "file full path";
         A.CallTo(_dialogCreator).WithReturnType<string>().Returns(_fileFullPath);

         A.CallTo(() => _simulationResultsToDataTableConverter.PKAnalysesToDataTableAsync(_populationSimulation.PKAnalyses, _populationSimulation)).Returns(Task.FromResult(_dataTable));
      }

      [Observation]
      public async Task should_load_the_simulation()
      {
         await sut.ExportPKAnalysesToCSVAsync(_populationSimulation);
         A.CallTo(() => _lazyLoadTask.Load(_populationSimulation)).MustHaveHappened();
      }

      [Observation]
      public async Task should_export_the_pk_analysis_to_csv()
      {
         await sut.ExportPKAnalysesToCSVAsync(_populationSimulation);
         A.CallTo(() => _simulationResultsToDataTableConverter.PKAnalysesToDataTableAsync(_populationSimulation.PKAnalyses, _populationSimulation)).MustHaveHappened();
      }
   }
}