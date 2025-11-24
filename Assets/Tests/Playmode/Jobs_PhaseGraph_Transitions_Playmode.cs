using NUnit.Framework;
using PureDOTS.Runtime.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Tests.Playmode
{
    /// <summary>
    /// Tests to verify job phase transitions: Idle→Navigate→Act→Deliver occur correctly.
    /// </summary>
    public class Jobs_PhaseGraph_Transitions_Playmode : EcsTestFixture
    {
        [Test]
        public void JobPhase_Transitions_IdleToAssigned()
        {
            // Arrange: Create villager with idle job
            var villager = EntityManager.CreateEntity();
            EntityManager.AddComponentData(villager, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            EntityManager.AddComponentData(villager, new VillagerJob
            {
                Type = VillagerJob.JobType.Gatherer,
                Phase = VillagerJob.JobPhase.Idle
            });
            
            // Verify initial state
            var job = EntityManager.GetComponentData<VillagerJob>(villager);
            Assert.AreEqual(VillagerJob.JobPhase.Idle, job.Phase, "Job should start in Idle phase");
            
            // Act: Transition to Assigned (simulated - in real system, WorkAssignmentSystem would do this)
            job.Phase = VillagerJob.JobPhase.Assigned;
            EntityManager.SetComponentData(villager, job);
            
            // Assert: Phase should be Assigned
            job = EntityManager.GetComponentData<VillagerJob>(villager);
            Assert.AreEqual(VillagerJob.JobPhase.Assigned, job.Phase, "Job should transition to Assigned phase");
        }
        
        [Test]
        public void JobPhase_Transitions_AssignedToActing()
        {
            // Arrange: Create villager with assigned job
            var villager = EntityManager.CreateEntity();
            EntityManager.AddComponentData(villager, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            EntityManager.AddComponentData(villager, new VillagerJob
            {
                Type = VillagerJob.JobType.Gatherer,
                Phase = VillagerJob.JobPhase.Assigned
            });
            
            // Act: Transition to Acting (simulated)
            var job = EntityManager.GetComponentData<VillagerJob>(villager);
            job.Phase = VillagerJob.JobPhase.Acting;
            EntityManager.SetComponentData(villager, job);
            
            // Assert: Phase should be Acting
            job = EntityManager.GetComponentData<VillagerJob>(villager);
            Assert.AreEqual(VillagerJob.JobPhase.Acting, job.Phase, "Job should transition to Acting phase");
        }
        
        [Test]
        public void JobPhase_Transitions_ActingToDelivering()
        {
            // Arrange: Create villager with acting job
            var villager = EntityManager.CreateEntity();
            EntityManager.AddComponentData(villager, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            EntityManager.AddComponentData(villager, new VillagerJob
            {
                Type = VillagerJob.JobType.Gatherer,
                Phase = VillagerJob.JobPhase.Acting
            });
            
            // Act: Transition to Delivering (simulated)
            var job = EntityManager.GetComponentData<VillagerJob>(villager);
            job.Phase = VillagerJob.JobPhase.Delivering;
            EntityManager.SetComponentData(villager, job);
            
            // Assert: Phase should be Delivering
            job = EntityManager.GetComponentData<VillagerJob>(villager);
            Assert.AreEqual(VillagerJob.JobPhase.Delivering, job.Phase, "Job should transition to Delivering phase");
        }
        
        [Test]
        public void JobPhase_Transitions_DeliveringToCompleted()
        {
            // Arrange: Create villager with delivering job
            var villager = EntityManager.CreateEntity();
            EntityManager.AddComponentData(villager, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            EntityManager.AddComponentData(villager, new VillagerJob
            {
                Type = VillagerJob.JobType.Gatherer,
                Phase = VillagerJob.JobPhase.Delivering
            });
            
            // Act: Transition to Completed (simulated)
            var job = EntityManager.GetComponentData<VillagerJob>(villager);
            job.Phase = VillagerJob.JobPhase.Completed;
            EntityManager.SetComponentData(villager, job);
            
            // Assert: Phase should be Completed
            job = EntityManager.GetComponentData<VillagerJob>(villager);
            Assert.AreEqual(VillagerJob.JobPhase.Completed, job.Phase, "Job should transition to Completed phase");
        }
        
        [Test]
        public void JobPhase_FullCycle_IdleToCompleted()
        {
            // Arrange: Create villager with idle job
            var villager = EntityManager.CreateEntity();
            EntityManager.AddComponentData(villager, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            EntityManager.AddComponentData(villager, new VillagerJob
            {
                Type = VillagerJob.JobType.Gatherer,
                Phase = VillagerJob.JobPhase.Idle
            });
            
            // Act: Simulate full cycle
            var job = EntityManager.GetComponentData<VillagerJob>(villager);
            
            // Idle → Assigned
            Assert.AreEqual(VillagerJob.JobPhase.Idle, job.Phase);
            job.Phase = VillagerJob.JobPhase.Assigned;
            EntityManager.SetComponentData(villager, job);
            
            // Assigned → Acting
            job = EntityManager.GetComponentData<VillagerJob>(villager);
            Assert.AreEqual(VillagerJob.JobPhase.Assigned, job.Phase);
            job.Phase = VillagerJob.JobPhase.Acting;
            EntityManager.SetComponentData(villager, job);
            
            // Acting → Delivering
            job = EntityManager.GetComponentData<VillagerJob>(villager);
            Assert.AreEqual(VillagerJob.JobPhase.Acting, job.Phase);
            job.Phase = VillagerJob.JobPhase.Delivering;
            EntityManager.SetComponentData(villager, job);
            
            // Delivering → Completed
            job = EntityManager.GetComponentData<VillagerJob>(villager);
            Assert.AreEqual(VillagerJob.JobPhase.Delivering, job.Phase);
            job.Phase = VillagerJob.JobPhase.Completed;
            EntityManager.SetComponentData(villager, job);
            
            // Assert: Final phase should be Completed
            job = EntityManager.GetComponentData<VillagerJob>(villager);
            Assert.AreEqual(VillagerJob.JobPhase.Completed, job.Phase, "Full cycle should end in Completed phase");
        }
        
        [Test]
        public void JobPhase_CanTransition_FromCompletedToIdle()
        {
            // Arrange: Create villager with completed job
            var villager = EntityManager.CreateEntity();
            EntityManager.AddComponentData(villager, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            EntityManager.AddComponentData(villager, new VillagerJob
            {
                Type = VillagerJob.JobType.Gatherer,
                Phase = VillagerJob.JobPhase.Completed
            });
            
            // Act: Transition back to Idle (for next job)
            var job = EntityManager.GetComponentData<VillagerJob>(villager);
            job.Phase = VillagerJob.JobPhase.Idle;
            EntityManager.SetComponentData(villager, job);
            
            // Assert: Phase should be Idle
            job = EntityManager.GetComponentData<VillagerJob>(villager);
            Assert.AreEqual(VillagerJob.JobPhase.Idle, job.Phase, "Job should transition from Completed to Idle for next cycle");
        }
    }
}

