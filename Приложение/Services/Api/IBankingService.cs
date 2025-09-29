using ATM.Models;

namespace ATM.Services.Api;

public interface IBankingService {
    (bool IsAuthenticated, AccountId? AccountId) Authenticate(CardData cardData, Pin pin);
    decimal GetBalance(AccountId accountId);
    bool ExecuteWithdrawal(AccountId accountId, decimal amount);
}