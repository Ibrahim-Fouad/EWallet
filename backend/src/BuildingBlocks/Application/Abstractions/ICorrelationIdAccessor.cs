namespace EWallet.BuildingBlocks.Application.Abstractions;

public interface ICorrelationIdAccessor
{
    Guid CorrelationId { get; }
    void Set(Guid correlationId);
}
