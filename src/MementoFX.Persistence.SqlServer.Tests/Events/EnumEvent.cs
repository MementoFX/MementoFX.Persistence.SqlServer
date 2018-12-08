namespace MementoFX.Persistence.SqlServer.Tests.Events
{
    public class EnumEvent : DomainEvent
    {
        public EnumEvent(TestEnum requiredConfirm, TestEnum? nullableConfirm) 
        {
            this.RequiredConfirm = requiredConfirm;
            this.NullableConfirm = nullableConfirm;
        }

        public TestEnum RequiredConfirm { get; private set; }

        public TestEnum? NullableConfirm { get; private set; }

        public enum TestEnum
        {
            Yes,
            No
        }
    }
}
