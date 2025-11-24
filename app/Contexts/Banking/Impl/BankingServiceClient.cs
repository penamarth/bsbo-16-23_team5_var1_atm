using ATM.Kernel.Common;
using ATM.Kernel.Models;
using ATM.Contexts.Banking;

namespace ATM.Contexts.Banking;

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

    public bool ChangePin(CardData cardData, Pin oldPin, Pin newPin) {
        Logger.Log($"Смена PIN для карты {cardData.CardNumber}...");
        // In real system, verify oldPin again and update.
        return true;
    }

    public bool Deposit(AccountId accountId, decimal amount) {
        Logger.Log($"Внесение {amount:C} на счет {accountId.Value}...");
        if (amount <= 0) return false;
        
        if (!_accountBalances.ContainsKey(accountId.Value)) {
            _accountBalances[accountId.Value] = 0m;
        }
        _accountBalances[accountId.Value] += amount;
        return true;
    }

    public bool Transfer(AccountId fromAccount, string toCardNumber, decimal amount) {
        Logger.Log($"Перевод {amount:C} со счета {fromAccount.Value} на карту {toCardNumber}...");
        if (amount <= 0) return false;

        if (!_accountBalances.TryGetValue(fromAccount.Value, out var balance)) return false;
        if (balance < amount) {
            Logger.Log("Отклонено: недостаточно средств для перевода.", LogLevel.Warning);
            return false;
        }

        _accountBalances[fromAccount.Value] -= amount;
        // In mock, we don't update the target account as we don't have its ID mapping easily here,
        // or we could simulate it if we had it. For now just deduct.
        return true;
    }
}