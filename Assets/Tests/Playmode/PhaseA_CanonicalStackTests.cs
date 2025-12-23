using NUnit.Framework;
using PureDOTS.Runtime.Comms;
using PureDOTS.Runtime.Communication;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Interrupts;
using PureDOTS.Runtime.Perception;
using PureDOTS.Runtime.Performance;
using PureDOTS.Systems.Communication;
using PureDOTS.Systems.Comms;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Tests.Playmode
{
    public class PhaseACanonicalStackTests
    {
        private World _world;
        private World _previousWorld;
        private EntityManager _entityManager;
        private Entity _commsStreamEntity;

        [SetUp]
        public void SetUp()
        {
            _world = new World("PhaseA_CanonicalStackTests");
            _previousWorld = World.DefaultGameObjectInjectionWorld;
            World.DefaultGameObjectInjectionWorld = _world;
            _entityManager = _world.EntityManager;

            EnsureSingleton(new TimeState
            {
                Tick = 10,
                FixedDeltaTime = 1f / 60f,
                DeltaTime = 1f / 60f,
                DeltaSeconds = 1f / 60f,
                CurrentSpeedMultiplier = 1f,
                ElapsedTime = 1f / 60f,
                WorldSeconds = 1f / 60f,
                IsPaused = false
            });

            EnsureSingleton(new RewindState
            {
                Mode = RewindMode.Record,
                TargetTick = 0,
                TickDuration = 1f / 60f,
                MaxHistoryTicks = 256,
                PendingStepTicks = 0
            });

            EnsureSingleton(UniversalPerformanceBudget.CreateDefaults());
            EnsureSingleton(new UniversalPerformanceCounters());
            EnsureSingleton(SimulationFeatureFlags.Default);

            _commsStreamEntity = _entityManager.CreateEntity(typeof(CommsMessageStreamTag));
            _entityManager.AddBuffer<CommsMessage>(_commsStreamEntity);
            _entityManager.AddBuffer<CommsMessageSemantic>(_commsStreamEntity);
        }

        [TearDown]
        public void TearDown()
        {
            if (_world != null)
            {
                if (World.DefaultGameObjectInjectionWorld == _world)
                {
                    World.DefaultGameObjectInjectionWorld = _previousWorld;
                }

                _world.Dispose();
                _world = null;
            }
        }

        [Test]
        public void CommunicationBridge_WritesOutboxAndSemantics()
        {
            var receiver = _entityManager.CreateEntity();
            var sender = CreateEndpointEntity();

            var requests = _entityManager.GetBuffer<CommSendRequest>(sender);
            var payload = new FixedString64Bytes("order.move");
            requests.Add(new CommSendRequest
            {
                Receiver = receiver,
                MessageType = CommMessageType.Order,
                TrueIntent = CommunicationIntent.RequestHelp,
                StatedIntent = CommunicationIntent.RequestHelp,
                PayloadId = payload,
                TransportMask = PerceptionChannel.EM,
                DeceptionStrength = 0f,
                AckPolicy = CommAckPolicy.Required,
                RedundancyLevel = 2,
                CommOrderVerb = CommOrderVerb.MoveTo,
                OrderTarget = Entity.Null,
                OrderTargetPosition = new float3(5f, 0f, 0f),
                OrderSide = CommOrderSide.Center,
                OrderPriority = CommOrderPriority.High,
                TimingWindowTicks = 30,
                ContextHash = 1234,
                Timestamp = 10
            });

            RunSystem<CommunicationToCommsBridgeSystem>();

            var outbox = _entityManager.GetBuffer<CommsOutboxEntry>(sender);
            Assert.AreEqual(1, outbox.Length);
            Assert.AreEqual(receiver, outbox[0].IntendedReceiver);
            Assert.AreEqual(InterruptType.CommsMessageReceived, outbox[0].InterruptType);

            var semantics = _entityManager.GetBuffer<CommsMessageSemantic>(_commsStreamEntity);
            Assert.AreEqual(1, semantics.Length);
            Assert.AreEqual(CommMessageType.Order, semantics[0].MessageType);
            Assert.AreEqual(CommOrderVerb.MoveTo, semantics[0].OrderVerb);
            Assert.AreEqual(CommunicationIntent.RequestHelp, semantics[0].StatedIntent);
            Assert.AreEqual(receiver, semantics[0].IntendedReceiver);
        }

        [Test]
        public void CommunicationBridge_EndToEndProducesCommReceipt()
        {
            var receiver = _entityManager.CreateEntity(typeof(CommEndpoint));
            _entityManager.AddBuffer<CommReceipt>(receiver);
            _entityManager.AddBuffer<CommsInboxEntry>(receiver);

            var sender = CreateEndpointEntity();
            var requests = _entityManager.GetBuffer<CommSendRequest>(sender);
            var payload = new FixedString64Bytes("order.attack");
            requests.Add(new CommSendRequest
            {
                Receiver = receiver,
                MessageType = CommMessageType.Order,
                TrueIntent = CommunicationIntent.Command,
                StatedIntent = CommunicationIntent.Command,
                PayloadId = payload,
                TransportMask = PerceptionChannel.EM,
                DeceptionStrength = 0f,
                AckPolicy = CommAckPolicy.Required,
                RedundancyLevel = 1,
                CommOrderVerb = CommOrderVerb.Attack,
                OrderTarget = Entity.Null,
                OrderTargetPosition = new float3(2f, 0f, 0f),
                OrderSide = CommOrderSide.Front,
                OrderPriority = CommOrderPriority.Critical,
                TimingWindowTicks = 45,
                ContextHash = 9876,
                Timestamp = 20
            });

            RunSystem<CommunicationToCommsBridgeSystem>();

            var semantics = _entityManager.GetBuffer<CommsMessageSemantic>(_commsStreamEntity);
            Assert.AreEqual(1, semantics.Length);
            var token = semantics[0].Token;

            var inbox = _entityManager.GetBuffer<CommsInboxEntry>(receiver);
            inbox.Add(new CommsInboxEntry
            {
                Token = token,
                Sender = sender,
                ReceivedTick = 25,
                SourceEmittedTick = 20,
                IntendedInterrupt = InterruptType.NewOrder,
                Priority = InterruptPriority.Critical,
                PayloadId = new FixedString32Bytes("order.attack"),
                TransportUsed = PerceptionChannel.EM,
                Integrity01 = 0.92f,
                MisreadSeverity = MiscommunicationSeverity.None,
                MisreadType = MiscommunicationType.None,
                Origin = float3.zero
            });

            RunSystem<CommsToCommunicationBridgeSystem>();

            var receipts = _entityManager.GetBuffer<CommReceipt>(receiver);
            Assert.AreEqual(1, receipts.Length);
            var receipt = receipts[0];
            Assert.AreEqual(CommMessageType.Order, receipt.MessageType);
            Assert.AreEqual(CommOrderVerb.Attack, receipt.OrderVerb);
            Assert.AreEqual(CommunicationIntent.Command, receipt.Intent);
            Assert.AreEqual(CommAckPolicy.Required, receipt.AckPolicy);
            Assert.AreEqual(0.92f, receipt.Integrity, 1e-4f);
            Assert.AreEqual(9876u, receipt.ContextHash);

            Assert.AreEqual(0, _entityManager.GetBuffer<CommsMessageSemantic>(_commsStreamEntity).Length);
        }

        private Entity CreateEndpointEntity()
        {
            var entity = _entityManager.CreateEntity(typeof(CommEndpoint));
            _entityManager.SetComponentData(entity, CommEndpoint.Default);
            _entityManager.AddBuffer<CommSendRequest>(entity);
            _entityManager.AddBuffer<CommsOutboxEntry>(entity);
            return entity;
        }

        private Entity EnsureSingleton<T>(T data) where T : unmanaged, IComponentData
        {
            using var query = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<T>());
            Entity entity;
            if (query.IsEmptyIgnoreFilter)
            {
                entity = _entityManager.CreateEntity(typeof(T));
            }
            else
            {
                entity = query.GetSingletonEntity();
            }

            _entityManager.SetComponentData(entity, data);
            return entity;
        }

        private void RunSystem<T>() where T : unmanaged, ISystem
        {
            var handle = _world.GetOrCreateSystem<T>();
            handle.Update(_world.Unmanaged);
        }
    }
}

