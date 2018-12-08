using System;
using System.Collections.Generic;

namespace MementoFX.Persistence.SqlServer.Tests.Events
{
    public class ComplexEvent : DomainEvent
    {
        public ComplexEvent(Guid complexId, int n, string title, InternalProp myProp, byte b)
        {
            this.ComplexId = complexId;
            this.N = n;
            this.Title = title;
            this.MyProp = myProp;
            this.B = b;
        }

        public Guid ComplexId { get; private set; }
        public int N { get; private set; }
        public string Title { get; private set; }
        public InternalProp MyProp { get; private set; }
        public byte B { get; private set; }

        public class InternalProp
        {
            public InternalProp(List<string> stringList)
            {
                this.StringList = stringList;
            }

            public List<string> StringList { get; private set; }
        }
    }
}
