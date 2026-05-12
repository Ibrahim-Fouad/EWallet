using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.BuildingBlocks.Common;
using EWallet.BuildingBlocks.Common.Constants;
using EWallet.BuildingBlocks.Infrastructure.Contracts;
using EWallet.Modules.Transactions.Application.Abstractions;
using EWallet.Modules.Transactions.Application.Sagas;
using EWallet.Modules.Transactions.Domain.Entities;
using EWallet.Modules.Transactions.Domain.Errors;
using EWallet.Modules.Transactions.Domain.Repositories;

namespace EWallet.Modules.Transactions.Application.Commands.Transfer;

internal sealed class TransferCommandHandler(
    IIdempotencyService idempotencyService,
    IWalletLookupService walletLookupService,
    ITransactionRepository transactionRepository,
    ITransactionUnitOfWork transactionUnitOfWork,
    IEventBus eventBus)
    : ICommandHandler<TransferCommand, TransferResponse>
{
    public async Task<Result<TransferResponse>> Handle(
        TransferCommand request,
        CancellationToken cancellationToken)
    {
        // 1 — Redis idempotency cache (hot path — same key hits this immediately)
        var cached = await idempotencyService.GetAsync(request.IdempotencyKey, cancellationToken);
        if (cached is not null)
            return Result.Success(cached);

        // 2 — DB fallback idempotency: handles the case where the process crashed
        //     after committing the Transaction row but before writing to Redis.
        var existing = await transactionRepository.GetByIdempotencyKeyAsync(
            request.IdempotencyKey, cancellationToken);

        if (existing is not null)
        {
            var existingResponse = new TransferResponse(
                existing.Id,
                existing.Status.ToString(),
                existing.Amount,
                existing.Currency);

            await idempotencyService.SetAsync(request.IdempotencyKey, existingResponse, cancellationToken);
            return Result.Success(existingResponse);
        }

        // 3 — Validate source wallet ownership
        var sourceInfo = await walletLookupService.GetByPhoneNumberAsync(request.SourcePhoneNumber, cancellationToken);
        if (sourceInfo.IsFailure)
            return Result.Failure<TransferResponse>(TransactionErrors.SourceWalletNotFound);

        if (sourceInfo.Value.OwnerId != request.RequestingUserId)
            return Result.Failure<TransferResponse>(
                Error.Unauthorized("Transaction.Unauthorized", "You do not own the source wallet."));

        // 4 — Validate destination wallet
        var destInfo = await walletLookupService.GetByPhoneNumberAsync(request.DestinationPhoneNumber, cancellationToken);
        if (destInfo.IsFailure)
            return Result.Failure<TransferResponse>(TransactionErrors.DestinationWalletNotFound);

        // 5 — Currency must match
        if (sourceInfo.Value.Currency != destInfo.Value.Currency)
            return Result.Failure<TransferResponse>(TransactionErrors.CurrencyMismatch);

        // 6 — Create Transaction in Pending state
        var description = request.DescriptionOverride
            ?? $"transfer {request.Amount} from {DisplayName(sourceInfo.Value.PhoneNumber)} to {DisplayName(destInfo.Value.PhoneNumber)}";

        var destinationDisplay = request.DestinationDisplayOverride
            ?? destInfo.Value.PhoneNumber;

        var transaction = Transaction.Create(
            request.IdempotencyKey,
            sourceInfo.Value.Id,
            destInfo.Value.Id,
            request.Amount,
            sourceInfo.Value.Currency,
            description,
            destinationDisplay,
            request.Notes);

        transactionRepository.Add(transaction);

        // 7 — Commit the Transaction row first.
        //     The DB-fallback idempotency check (step 2) covers the window between
        //     this commit and the publish below — if the process crashes here, the
        //     next client retry returns the Pending response without a duplicate row.
        await transactionUnitOfWork.SaveChangesAsync(cancellationToken);

        // 8 — Trigger the saga. Uses IPublishEndpoint → RabbitMQ (direct).
        //     If publish fails here, the Transaction stays Pending in the DB.
        //     The Hangfire reconciliation job (Step 9) will detect stuck-Pending
        //     transactions and re-publish this message for recovery.
        await eventBus.PublishAsync(new TransferRequestedMessage(
            transaction.Id,          // CorrelationId = TransactionId (one saga per transfer)
            transaction.Id,
            sourceInfo.Value.Id,
            destInfo.Value.Id,
            request.Amount,
            sourceInfo.Value.Currency,
            request.Origin), cancellationToken);

        // 9 — Cache the pending response so subsequent retries with the same
        //     idempotency key don't go through validation again.
        var response = new TransferResponse(
            transaction.Id,
            transaction.Status.ToString(),  // "Pending" — saga will drive it to Completed/Failed
            transaction.Amount,
            transaction.Currency);

        await idempotencyService.SetAsync(request.IdempotencyKey, response, cancellationToken);

        return Result.Success(response);
    }

    private static string DisplayName(string phoneNumber) =>
        phoneNumber.StartsWith("SYSTEM-", StringComparison.OrdinalIgnoreCase) ? "System" : phoneNumber;
}
