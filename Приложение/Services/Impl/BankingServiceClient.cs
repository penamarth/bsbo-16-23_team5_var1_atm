using ATM.Common;
using ATM.Models;
using ATM.Services.Api;

namespace ATM.Services.Impl;

public class BankingServiceClient : IBankingService
{
    private readonly Dictionary<Guid, decimal> _accountBalances = new();
    private readonly Dictionary<Guid, (DateOnly Date, decimal Withdrawn)> _dailyWithdrawals = new();
    private const decimal DefaultInitialBalance = 15000m;
    private const decimal DailyLimit = 10000m; 

    public (bool IsAuthenticated, AccountId? AccountId) Authenticate(CardData cardData, Pin pin) {
        Logger.Log($"Аутентификация для карты {cardData.CardNumber}...");
        var accountId = new AccountId(Guid.NewGuid());
        if (!_accountBalances.ContainsKey(accountId.Value))
        {
            _accountBalances[accountId.Value] = DefaultInitialBalance;
        }
        return (true, accountId);
    }

    public decimal GetBalance(AccountId accountId) {
        Logger.Log($"Запрос баланса для счета {accountId.Value}...");
        return _accountBalances.TryGetValue(accountId.Value, out var bal) ? bal : 0m;
    }

    public bool ExecuteWithdrawal(AccountId accountId, decimal amount) {
        Logger.Log($"Списание {amount:C} со счета {accountId.Value}...");
        if (amount <= 0) return false;

        if (!_accountBalances.TryGetValue(accountId.Value, out var balance))
        {
            balance = 0m;
        }

        if (amount > balance)
        {
            Logger.Log("Отклонено: недостаточно средств.", LogLevel.Warning);
            return false;
        }

        var today = DateOnly.FromDateTime(DateTime.Now);
        var daily = _dailyWithdrawals.TryGetValue(accountId.Value, out var dw) && dw.Date == today
            ? dw.Withdrawn
            : 0m;
        if (daily + amount > DailyLimit)
        {
            Logger.Log("Отклонено: превышен суточный лимит.", LogLevel.Warning);
            return false;
        }

        _accountBalances[accountId.Value] = balance - amount;
        _dailyWithdrawals[accountId.Value] = (today, daily + amount);
        return true;
    }
}