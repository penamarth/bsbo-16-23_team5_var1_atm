using ATM.Kernel.Models;
using ATM.Kernel.Models;

namespace ATM.Contexts.Banking;

public interface IBankingService {
    (bool IsAuthenticated, AccountId? AccountId) Authenticate(CardData cardData, Pin pin);
    decimal GetBalance(AccountId accountId);
    bool ExecuteWithdrawal(AccountId accountId, decimal amount);
    bool ChangePin(CardData cardData, Pin oldPin, Pin newPin);
    bool Deposit(AccountId accountId, decimal amount);
    bool Transfer(AccountId fromAccount, string toCardNumber, decimal amount);
}