namespace ATM.Models {
    public readonly record struct CardData(string CardNumber);
    public readonly record struct Pin(string Value);
    public readonly record struct AccountId(Guid Value);
}