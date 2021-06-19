using MediatR;
using OperationResult;
using System;

namespace EventBus.Core
{
    public abstract class IntegrationEvent : IRequest<Result>
    {
        public IntegrationEvent() => Name = GetType().Name;

        public virtual Guid Id { get; } = Guid.NewGuid();

        public virtual DateTime Date { get; } = DateTime.Now;

        public virtual string Name { get; }
        public string? ConcurrenceId { get; protected set; }
    }
}