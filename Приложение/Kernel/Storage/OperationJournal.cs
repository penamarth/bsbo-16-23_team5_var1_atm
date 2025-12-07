using ATM.Kernel.Common;

namespace ATM.Kernel.Storage;

public enum OperationStatus
{
    Success,
    Failed,
    Skipped
}

public record OperationEntry(
    DateTime Timestamp,
    string Operation,
    OperationStatus Status,
    string? CardNumberMasked,
    Guid? AccountId,
    decimal? Amount,
    string? Details
)
{
    public static string MaskCard(string? cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber)) return null!;
        var lastDigits = cardNumber.Length <= 4 ? cardNumber : cardNumber[^4..];
        return $"****-****-****-{lastDigits}";
    }

    public static OperationEntry Create(string operation, OperationStatus status, string? cardNumber, Guid? accountId, decimal? amount, string? details) =>
        new(DateTime.Now, operation, status, MaskCard(cardNumber), accountId, amount, details);
}

public class OperationJournal
{
    private readonly LocalStorage _storage;
    private readonly string _fileName;
    private readonly List<OperationEntry> _entries;

    public OperationJournal(LocalStorage storage, string fileName = "operations.json")
    {
        _storage = storage;
        _fileName = fileName;
        _entries = _storage.LoadOrDefault(fileName, new List<OperationEntry>());
    }

    public IReadOnlyList<OperationEntry> Entries => _entries;

    public OperationEntry Add(OperationEntry entry)
    {
        _entries.Add(entry);
        Persist();
        Logger.Log($"Журнал: {entry.Operation} {entry.Status} {entry.Amount?.ToString("C") ?? string.Empty} {entry.CardNumberMasked}");
        return entry;
    }

    public void Persist() => _storage.Save(_fileName, _entries);
}

