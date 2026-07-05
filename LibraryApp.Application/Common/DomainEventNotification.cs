using LibraryApp.Domain.Common;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryApp.Application.Common
{
    public class DomainEventNotification <TDomainEvent> : INotification where TDomainEvent : IDomainEvent
    {
        public TDomainEvent DomainEvent { get; set; }
        public DomainEventNotification(TDomainEvent domainEvent) => DomainEvent = domainEvent;
    }
}
