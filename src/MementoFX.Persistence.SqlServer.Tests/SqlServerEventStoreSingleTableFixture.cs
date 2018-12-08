using MementoFX.Messaging;
using MementoFX.Persistence.SqlServer.Tests.Events;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MementoFX.Persistence.SqlServer.Tests
{
    public class SqlServerEventStoreSingleTableFixture
    {
        private readonly IEventDispatcher EventDispatcher;
        private readonly Configuration.Settings Settings;

        public SqlServerEventStoreSingleTableFixture()
        {
            this.EventDispatcher = new Mock<IEventDispatcher>().Object;
            this.Settings = new Configuration.Settings(Config.SingleTableConnectionString, useSingleTable: true);
        }

        [Fact]
        public void SqlServerEventStore_Ctor_Should_Not_Throw()
        {
            var exception = Record.Exception(() => new SqlServerEventStore(this.Settings, this.EventDispatcher));

            Assert.Null(exception);
        }

        [Fact]
        public void SqlServerEventStore_Ctor_Should_Not_Return_Null_Instance()
        {
            var eventStore = new SqlServerEventStore(this.Settings, this.EventDispatcher);

            Assert.NotNull(eventStore);
        }

        [Fact]
        public void SqlServerEventStore_Find_NotMappedEvent_Should_Return_Empty_Collection_If_Table_Does_Not_Exist()
        {
            var eventStore = new SqlServerEventStore(this.Settings, this.EventDispatcher);

            var events = eventStore.Find<NotMappedEvent>(p => p.Id == Guid.Empty);

            Assert.NotNull(events);
            Assert.Empty(events);
        }

        [Fact]
        public void SqlServerEventStore_Find_PlainEvent_Should_Work()
        {
            var eventStore = new SqlServerEventStore(this.Settings, this.EventDispatcher);

            var events = eventStore.Find<PlainEvent>(e => e.MyAggId != Guid.Empty).ToArray();

            Assert.NotNull(events);
            Assert.NotEmpty(events);
        }

        [Fact]
        public void SqlServerEventStore_Find_ComplexEvent_Should_Work()
        {
            var eventStore = new SqlServerEventStore(this.Settings, this.EventDispatcher);

            var events = eventStore.Find<ComplexEvent>(e => e.N > 0).ToArray();

            Assert.NotNull(events);
            Assert.NotEmpty(events);
        }

        [Fact]
        public void SqlServerEventStore_Find_PlainEvent_Should_Retrieve_Previously_Saved_Event()
        {
            var myAggId = Guid.NewGuid();

            var plainEvent = new PlainEvent(myAggId, null);

            var eventStore = new SqlServerEventStore(this.Settings, this.EventDispatcher);

            eventStore.Save(plainEvent);

            var events = eventStore.Find<PlainEvent>(e => e.MyAggId == myAggId).ToArray();

            Assert.NotNull(events);
            Assert.Single(events);

            var @event = events[0];
            Assert.Equal(myAggId, @event.MyAggId);
            Assert.Equal(plainEvent.Id, @event.Id);
            Assert.Equal(plainEvent.TimeStamp.ToLongDateString(), @event.TimeStamp.ToLongDateString());
            Assert.Null(@event.TimelineId);
        }

        [Fact]
        public void SqlServerEventStore_Save_PlainEvent_Should_Work()
        {
            var eventStore = new SqlServerEventStore(this.Settings, this.EventDispatcher);

            var guid = Guid.NewGuid();

            var plainEvent = new PlainEvent(guid, PlainEvent.TestEnum.Nope);

            var exception = Record.Exception(() => eventStore.Save(plainEvent));

            Assert.Null(exception);
        }

        [Fact]
        public void SqlServerEventStore_Save_ComplexEvent_Should_Work()
        {
            var eventStore = new SqlServerEventStore(this.Settings, this.EventDispatcher);

            var stringList = new List<string> { "Hello", "Memento" };

            var myProp = new ComplexEvent.InternalProp(stringList);

            var complexEvent = new ComplexEvent(Guid.NewGuid(), Environment.TickCount, "PROV@", myProp, byte.MaxValue);

            var exception = Record.Exception(() => eventStore.Save(complexEvent));

            Assert.Null(exception);
        }

        [Fact]
        public void SqlServerEventStore_RetrieveEvents_PlainEvent_Should_Work()
        {
            var eventStore = new SqlServerEventStore(this.Settings, this.EventDispatcher);

            var aggId = Guid.NewGuid();

            var events = new[]
            {
                new PlainEvent(aggId, PlainEvent.TestEnum.Nope),
                new PlainEvent(aggId, null),
                new PlainEvent(Guid.NewGuid(), null),
                new PlainEvent(aggId, PlainEvent.TestEnum.Ok),
            };

            var eventsCount = events.Count(e => e.MyAggId == aggId);

            foreach (var @event in events)
            {
                eventStore.Save(@event);
            }

            var eventMapping = new EventMapping { AggregateIdPropertyName = nameof(PlainEvent.MyAggId), EventType = typeof(PlainEvent) };

            var eventDescriptors = new[] { eventMapping };

            var savedEvents = eventStore.RetrieveEvents(aggId, DateTime.Now, eventDescriptors, timelineId: null);

            Assert.NotNull(savedEvents);
            Assert.NotEmpty(savedEvents);
            Assert.True(savedEvents.All(e => e is PlainEvent));

            Assert.Equal(eventsCount, savedEvents.Count());
        }

        [Fact]
        public void SqlServerEventStore_RetrieveEvents_ComplexEvent_Should_Work()
        {
            var eventStore = new SqlServerEventStore(this.Settings, this.EventDispatcher);

            var complexId = Guid.NewGuid();

            var events = new[]
            {
                new ComplexEvent(complexId, Environment.TickCount, "PROV@", new ComplexEvent.InternalProp(new List<string> { "Hello", "Memento" }), byte.MaxValue),
                new ComplexEvent(complexId, 10000, "test titolo", new ComplexEvent.InternalProp(new List<string>()), byte.MinValue),
                new ComplexEvent(Guid.NewGuid(), 8787878, null, null, 0),
                new ComplexEvent(complexId, 12345, string.Empty, new ComplexEvent.InternalProp(new List<string> { "test" }), 128),
            };

            var eventsCount = events.Count(e => e.ComplexId == complexId);

            foreach (var @event in events)
            {
                eventStore.Save(@event);
            }

            var eventMapping = new EventMapping { AggregateIdPropertyName = nameof(ComplexEvent.ComplexId), EventType = typeof(ComplexEvent) };

            var eventDescriptors = new[] { eventMapping };

            var savedEvents = eventStore.RetrieveEvents(complexId, DateTime.Now, eventDescriptors, timelineId: null);

            Assert.NotNull(savedEvents);
            Assert.NotEmpty(savedEvents);
            Assert.True(savedEvents.All(e => e is ComplexEvent));

            Assert.Equal(eventsCount, savedEvents.Count());
        }
    }
}
