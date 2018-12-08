using MementoFX.Messaging;
using MementoFX.Persistence.SqlServer.Tests.Events;
using Moq;
using System.Linq;
using Xunit;

namespace MementoFX.Persistence.SqlServer.Tests
{
    public class SqlTypesFixture
    {
        private readonly IEventDispatcher EventDispatcher;

        public SqlTypesFixture()
        {
            this.EventDispatcher = new Mock<IEventDispatcher>().Object;
        }

        [Fact]
        public void SqlServerEventStore_Save_And_Find_Should_Retrieve_EnumEvent_Without_Throwing_When_NullableConfirm_Is_Null()
        {
            var enumEvent = new EnumEvent(EnumEvent.TestEnum.Yes, nullableConfirm: null);
            
            var eventStore = new SqlServerEventStore(Config.ConnectionString, this.EventDispatcher);

            eventStore.Save(enumEvent);
            
            var events = eventStore.Find<EnumEvent>(e => enumEvent.Id == e.Id).ToArray();

            Assert.NotNull(events);
            Assert.Single(events);

            var @event = events[0];

            Assert.Equal(enumEvent.Id, @event.Id);
            Assert.Equal(enumEvent.TimelineId, @event.TimelineId);
            Assert.Equal(enumEvent.TimeStamp.ToLongDateString(), @event.TimeStamp.ToLongDateString());
            Assert.Equal(enumEvent.RequiredConfirm, @event.RequiredConfirm);
            Assert.Equal(enumEvent.NullableConfirm, @event.NullableConfirm);
            Assert.Null(@event.NullableConfirm);
        }

        [Fact]
        public void SqlServerEventStore_Save_And_Find_Should_Retrieve_EnumEvent_Without_Throwing_When_NullableConfirm_Is_Not_Null()
        {
            var enumEvent = new EnumEvent(EnumEvent.TestEnum.Yes, nullableConfirm: EnumEvent.TestEnum.No);

            var eventId = enumEvent.Id;

            var eventStore = new SqlServerEventStore(Config.ConnectionString, this.EventDispatcher);

            eventStore.Save(enumEvent);

            System.Threading.Thread.Sleep(1000);

            var events = eventStore.Find<EnumEvent>(e => e.Id == eventId).ToArray();

            Assert.NotNull(events);
            Assert.Single(events);

            var @event = events[0];

            Assert.Equal(enumEvent.Id, @event.Id);
            Assert.Equal(enumEvent.TimelineId, @event.TimelineId);
            Assert.Equal(enumEvent.TimeStamp.ToLongDateString(), @event.TimeStamp.ToLongDateString());
            Assert.Equal(enumEvent.RequiredConfirm, @event.RequiredConfirm);
            Assert.Equal(enumEvent.NullableConfirm, @event.NullableConfirm);
            Assert.NotNull(@event.NullableConfirm);
        }
    }
}
