using ATM.Common;
using ATM.Models;
using ATM.Services.Api;

namespace ATM.Services.Impl;

public class BankingServiceClient : IBankingService
{
    public (bool IsAuthenticated, AccountId? AccountId) Authenticate(CardData cardData, Pin pin) {
        Logger.Log($"Аутентификация для карты {cardData.CardNumber}...");
        var accountId = new AccountId(Guid.NewGuid());
        return (true, accountId);
    }

    public decimal GetBalance(AccountId accountId) {
        Logger.Log($"Запрос баланса для счета {accountId.Value}...");
        return 12345.67m;
    }

    public bool ExecuteWithdrawal(AccountId accountId, decimal amount) {
        Logger.Log($"Списание {amount:C} со счета {accountId.Value}...");
        return true;
    }
}