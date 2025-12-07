namespace ATM.Kernel.Models;

public readonly record struct AtmSession(AccountId AccountId, CardData Card, Pin Pin);

