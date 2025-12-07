using ATM.Kernel.Common;
using ATM.Kernel.Models;
using ATM.Kernel.Storage;

namespace ATM.Contexts.Banking;

public class BankingServiceClient : IBankingService
{
    private readonly LocalStorage _storage;
    private readonly Dictionary<Guid, decimal> _accountBalances;
    private readonly Dictionary<Guid, (DateOnly Date, decimal Withdrawn)> _dailyWithdrawals;
    private readonly Dictionary<string, Guid> _cardBindings;
    private readonly Dictionary<string, string> _cardPins;

    private const decimal DefaultInitialBalance = 15000m;
    private const decimal DailyLimit = 10000m; 
    private const string AccountsFile = "bank_accounts.json";
    private const string WithdrawalsFile = "bank_daily_withdrawals.json";
    private const string CardBindingsFile = "bank_card_bindings.json";
    private const string CardPinsFile = "bank_card_pins.json";

    public BankingServiceClient(LocalStorage? storage = null)
    {
        _storage = storage ?? new LocalStorage();
        _accountBalances = _storage.LoadOrDefault(AccountsFile, new Dictionary<Guid, decimal>());
        _dailyWithdrawals = _storage.LoadOrDefault(WithdrawalsFile, new Dictionary<Guid, (DateOnly, decimal)>());
        _cardBindings = _storage.LoadOrDefault(CardBindingsFile, new Dictionary<string, Guid>());
        _cardPins = _storage.LoadOrDefault(CardPinsFile, new Dictionary<string, string>());
    }

    public (bool IsAuthenticated, AccountId? AccountId) Authenticate(CardData cardData, Pin pin) {
        Logger.Log($"Аутентификация для карты {cardData.CardNumber}...");

        if (!_cardBindings.TryGetValue(cardData.CardNumber, out var accountGuid))
        {
            accountGuid = Guid.NewGuid();
            _cardBindings[cardData.CardNumber] = accountGuid;
            _accountBalances[accountGuid] = DefaultInitialBalance;
            _cardPins[cardData.CardNumber] = pin.Value;
            Persist();
            Logger.Log("Создан новый счет и сохранен в локальном хранилище.");
        }

        var storedPin = _cardPins.GetValueOrDefault(cardData.CardNumber);
        var isAuthenticated = storedPin == pin.Value;
        if (!isAuthenticated)
        {
            Logger.Log("Ошибка аутентификации: неверный PIN.", LogLevel.Warning);
            return (false, null);
        }

        return (true, new AccountId(accountGuid));
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
        Persist();
        return true;
    }

    public bool ChangePin(CardData cardData, Pin oldPin, Pin newPin) {
        Logger.Log($"Смена PIN для карты {cardData.CardNumber}...");
        var storedPin = _cardPins.GetValueOrDefault(cardData.CardNumber);
        if (storedPin != oldPin.Value)
        {
            Logger.Log("Смена PIN отклонена: старый PIN не совпал.", LogLevel.Warning);
            return false;
        }

        _cardPins[cardData.CardNumber] = newPin.Value;
        Persist();
        return true;
    }

    public bool Deposit(AccountId accountId, decimal amount) {
        Logger.Log($"Внесение {amount:C} на счет {accountId.Value}...");
        if (amount <= 0) return false;
        
        if (!_accountBalances.ContainsKey(accountId.Value)) {
            _accountBalances[accountId.Value] = 0m;
        }
        _accountBalances[accountId.Value] += amount;
        Persist();
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

        _accountBalances[fromAccount.Value] = balance - amount;
        if (_cardBindings.TryGetValue(toCardNumber, out var toAccount))
        {
            _accountBalances[toAccount] = _accountBalances.GetValueOrDefault(toAccount, 0m) + amount;
        }
        Persist();
        return true;
    }

    private void Persist()
    {
        _storage.Save(AccountsFile, _accountBalances);
        _storage.Save(WithdrawalsFile, _dailyWithdrawals);
        _storage.Save(CardBindingsFile, _cardBindings);
        _storage.Save(CardPinsFile, _cardPins);
    }
}