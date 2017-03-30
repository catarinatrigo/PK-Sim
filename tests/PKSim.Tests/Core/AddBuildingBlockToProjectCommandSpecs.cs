using OSPSuite.BDDHelper;
using OSPSuite.BDDHelper.Extensions;
using OSPSuite.Core.Commands.Core;
using PKSim.Core.Commands;
using PKSim.Core.Model;
using FakeItEasy;

namespace PKSim.Core
{
   public abstract class concern_for_AddBuildingBlockToProjectCommand : ContextSpecification<AddBuildingBlockToProjectCommand>
   {
      protected IExecutionContext _executionContext;
      protected IPKSimBuildingBlock _buildingBlock;

      protected override void Context()
      {
         _executionContext = A.Fake<IExecutionContext>();
         _buildingBlock = A.Fake<IPKSimBuildingBlock>();
         sut = new AddBuildingBlockToProjectCommand(_buildingBlock, _executionContext);
      }
   }

   
   public class When_adding_a_building_block_to_a_project_with_the_command : concern_for_AddBuildingBlockToProjectCommand
   {
      protected override void Context()
      {
         base.Context();
         A.CallTo(() => _executionContext.CurrentProject).Returns(A.Fake<IPKSimProject>());
      }

      protected override void Because()
      {
         sut.Execute(_executionContext);
      }

      [Observation]
      public void should_add_the_building_block_to_the_project()
      {
         A.CallTo(() => _executionContext.CurrentProject.AddBuildingBlock(_buildingBlock)).MustHaveHappened();
      }
   }

   
   public class The_inverse_of_the_add_building_block_to_project_command : concern_for_AddBuildingBlockToProjectCommand
   {
      private IReversibleCommand<IExecutionContext> _result;

      protected override void Because()
      {
         _result = sut.InverseCommand(_executionContext);
      }

      [Observation]
      public void should_be_a_remove_building_block_from_project_command()
      {
         _result.ShouldBeAnInstanceOf<RemoveBuildingBlockFromProjectCommand>();
      }

      [Observation]
      public void should_have_beeen_marked_as_inverse_for_the_add_command()
      {
         _result.IsInverseFor(sut).ShouldBeTrue();
      }
   }
}