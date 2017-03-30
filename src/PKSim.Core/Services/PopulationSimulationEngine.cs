using System;
using System.Threading.Tasks;
using PKSim.Assets;
using OSPSuite.Utility;
using OSPSuite.Utility.Events;
using OSPSuite.Utility.Exceptions;
using OSPSuite.Utility.Extensions;
using PKSim.Core.Events;
using PKSim.Core.Model;
using OSPSuite.Core.Domain.Mappers;
using OSPSuite.Core.Domain.Services;
using OSPSuite.Core.Events;

namespace PKSim.Core.Services
{
   public class PopulationSimulationEngine : ISimulationEngine<PopulationSimulation>
   {
      private readonly IPopulationRunner _populationRunner;
      private readonly IProgressManager _progressManager;
      private IProgressUpdater _progressUpdater;
      private readonly IEventPublisher _eventPublisher;
      private readonly IExceptionManager _exceptionManager;
      private readonly ISimulationResultsSynchronizer _simulationResultsSynchronizer;
      private readonly IPopulationExportTask _populationExporter;
      private readonly ISimulationToModelCoreSimulationMapper _modelCoreSimulationMapper;
      private readonly ISimulationPersistableUpdater _simulationPersistableUpdater;
      private readonly ICoreUserSettings _userSettings;
      private readonly IPopulationSimulationAnalysisSynchronizer _populationSimulationAnalysisSynchronizer;

      public PopulationSimulationEngine(IPopulationRunner populationRunner, IProgressManager progressManager,
         IEventPublisher eventPublisher, IExceptionManager exceptionManager,
         ISimulationResultsSynchronizer simulationResultsSynchronizer, IPopulationExportTask populationExporter,
         ISimulationToModelCoreSimulationMapper modelCoreSimulationMapper, ISimulationPersistableUpdater simulationPersistableUpdater, ICoreUserSettings userSettings,
         IPopulationSimulationAnalysisSynchronizer populationSimulationAnalysisSynchronizer)
      {
         _populationRunner = populationRunner;
         _progressManager = progressManager;
         _eventPublisher = eventPublisher;
         _exceptionManager = exceptionManager;
         _simulationResultsSynchronizer = simulationResultsSynchronizer;
         _populationExporter = populationExporter;
         _modelCoreSimulationMapper = modelCoreSimulationMapper;
         _simulationPersistableUpdater = simulationPersistableUpdater;
         _userSettings = userSettings;
         _populationSimulationAnalysisSynchronizer = populationSimulationAnalysisSynchronizer;
         _populationRunner.Terminated += terminated;
         _populationRunner.SimulationProgress += simulationProgress;
      }

      public async Task RunAsync(PopulationSimulation populationSimulation)
      {
         _progressUpdater = _progressManager.Create();
         _progressUpdater.Initialize(populationSimulation.NumberOfItems, PKSimConstants.UI.Calculating);
         _populationRunner.NumberOfCoresToUse = _userSettings.MaximumNumberOfCoresToUse;

         //make sure that thread methods always catch and handle any exception,
         //otherwise we risk unplanned application termination
         var begin = SystemTime.UtcNow();
         try
         {
            var populationData = _populationExporter.CreatePopulationDataFor(populationSimulation);
            var modelCoreSimulation = _modelCoreSimulationMapper.MapFrom(populationSimulation, shouldCloneModel: false);
            _simulationPersistableUpdater.UpdatePersistableFromSettings(populationSimulation);

            _eventPublisher.PublishEvent(new SimulationRunStartedEvent());
            var populationRunResults = await _populationRunner.RunPopulationAsync(modelCoreSimulation, populationData, populationSimulation.AgingData.ToDataTable());
            _simulationResultsSynchronizer.Synchronize(populationSimulation, populationRunResults.Results);
            _populationSimulationAnalysisSynchronizer.UpdateAnalysesDefinedIn(populationSimulation);
            _eventPublisher.PublishEvent(new SimulationResultsUpdatedEvent(populationSimulation));
         }
         catch (OperationCanceledException)
         {
            simulationTerminated();
         }
         catch (Exception ex)
         {
            _exceptionManager.LogException(ex);
            simulationTerminated();
         }
         finally
         {
            var end = SystemTime.UtcNow();
            var timeSpent = end - begin;
            _eventPublisher.PublishEvent(new SimulationRunFinishedEvent(populationSimulation, timeSpent));
         }
      }

      private void simulationTerminated()
      {
         terminated(this, new EventArgs());
      }

      private void terminated(object sender, EventArgs e)
      {
         _progressUpdater?.Dispose();
         _populationRunner.Terminated -= terminated;
         _populationRunner.SimulationProgress -= simulationProgress;
      }

      public void Run(PopulationSimulation simulation)
      {
         RunAsync(simulation).Wait();
      }

      public void RunForBatch(PopulationSimulation populationSimulation, bool checkNegativeValues)
      {
         //no specific implementation for population run
         Run(populationSimulation);
      }

      public void Stop()
      {
         _populationRunner.StopSimulation();
      }

      private void simulationProgress(object sender, PopulationSimulationProgressEventArgs eventArgs)
      {
         _progressUpdater.ReportProgress(
            eventArgs.NumberOfCalculatedSimulation,
            PKSimConstants.UI.CalculationPopulationSimulation.FormatWith(eventArgs.NumberOfCalculatedSimulation, eventArgs.NumberOfSimulations));
      }
   }
}