namespace EWallet.BuildingBlocks.Application.Abstractions;

internal sealed class CorrelationIdAccessor : ICorrelationIdAccessor
{
    private Guid _id;
    private bool _set;

    public Guid CorrelationId => _set
        ? _id
        : throw new InvalidOperationException("CorrelationId has not been set for this scope.");

    public void Set(Guid correlationId)
    {
        if (_set) return;
        _id = correlationId;
        _set = true;
    }
}
