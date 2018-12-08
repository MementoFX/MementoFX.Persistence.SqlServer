using System;

namespace MementoFX.Persistence.SqlServer.Tests.Events
{
    public class PlainEvent : DomainEvent
    {
        public PlainEvent(Guid myAggId, TestEnum? test)
        {
            this.MyAggId = myAggId;
            this.Test = test;
        }

        public Guid MyAggId { get; private set; }

        public TestEnum? Test { get; private set; }

        public enum TestEnum
        {
            Ok,
            Nope
        }
    }
}
