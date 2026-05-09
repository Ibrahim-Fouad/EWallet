using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EWallet.Modules.Notifications.Infrastructure.Jobs;

public sealed class ReconciliationJob(
    IConfiguration configuration,
    ILogger<ReconciliationJob> logger)
{
    // System user/wallet IDs are well-known constants — excluded from user-facing reconciliation
    private static readonly Guid SystemUserId = new("00000000-0000-0000-0000-000000000001");

    public async Task RunAsync()
    {
        var connectionString = configuration.GetConnectionString("sqlserver")!;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var (userWalletCount, totalUserBalance, negativeBalanceCount) =
            await QueryWalletMetricsAsync(connection);

        var (completedTxCount, completedTxVolume) =
            await QueryTransactionMetricsAsync(connection);

        logger.LogInformation(
            "Reconciliation — UserWallets: {Count}, TotalBalance: {Balance:F4}, " +
            "NegativeBalances: {Negative}, CompletedTx24h: {TxCount}, Volume24h: {Volume:F4}",
            userWalletCount,
            totalUserBalance,
            negativeBalanceCount,
            completedTxCount,
            completedTxVolume);

        if (negativeBalanceCount > 0)
        {
            logger.LogWarning(
                "Reconciliation ALERT — {Count} wallet(s) have negative balances. Immediate investigation required.",
                negativeBalanceCount);
        }
    }

    private static async Task<(int walletCount, decimal totalBalance, int negativeCount)>
        QueryWalletMetricsAsync(SqlConnection connection)
    {
        const string sql = """
            SELECT
                COUNT(*),
                ISNULL(SUM(Balance), 0),
                SUM(CASE WHEN Balance < 0 THEN 1 ELSE 0 END)
            FROM wallets.wallets
            WHERE OwnerId != @systemUserId
              AND IsActive = 1
            """;

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@systemUserId", SystemUserId);

        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        return (reader.GetInt32(0), reader.GetDecimal(1), reader.GetInt32(2));
    }

    private static async Task<(int txCount, decimal volume)>
        QueryTransactionMetricsAsync(SqlConnection connection)
    {
        const string sql = """
            SELECT COUNT(*), ISNULL(SUM(Amount), 0)
            FROM transactions.transactions
            WHERE Status = 'Completed'
              AND CompletedAt >= DATEADD(hour, -24, SYSUTCDATETIME())
            """;

        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        return (reader.GetInt32(0), reader.GetDecimal(1));
    }
}
