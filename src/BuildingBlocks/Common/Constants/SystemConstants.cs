namespace EWallet.BuildingBlocks.Common.Constants;

public static class SystemConstants
{
    public static readonly Guid SystemUserId = new("00000000-0000-0000-0000-000000000001");
    public static readonly Guid SystemWalletEgpId = new("00000000-0000-0000-0000-000000000001");
    public static readonly Guid SystemWalletUsdId = new("00000000-0000-0000-0000-000000000002");

    public const string SystemPhoneEgp = "SYSTEM-EGP";
    public const string SystemPhoneUsd = "SYSTEM-USD";
    public const decimal WelcomeBonusAmount = 10m;
}
