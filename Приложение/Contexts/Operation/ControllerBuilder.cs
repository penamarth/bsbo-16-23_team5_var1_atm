using System.Reflection;
using ATM.Kernel.Hardware;
using ATM.Contexts.Banking;
using ATM.Kernel.Storage;

namespace ATM.Contexts.Operation;

public class ATMControllerBuilder {
    private CardReader? _cardReader;
    private CashDispenser? _cashDispenser;
    private CashAcceptor? _cashAcceptor;
    private IBankingService? _bankingService;
    private ReceiptPrinter? _receiptPrinter;
    private OperationJournal? _operationJournal;

    public ATMControllerBuilder WithCardReader(CardReader reader) { _cardReader = reader; return this; }
    public ATMControllerBuilder WithCashDispenser(CashDispenser dispenser) { _cashDispenser = dispenser; return this; }
    public ATMControllerBuilder WithCashAcceptor(CashAcceptor acceptor) { _cashAcceptor = acceptor; return this; }
    public ATMControllerBuilder WithBankingService(IBankingService service) { _bankingService = service; return this; }
    public ATMControllerBuilder WithReceiptPrinter(ReceiptPrinter printer) { _receiptPrinter = printer; return this; }
    public ATMControllerBuilder WithOperationJournal(OperationJournal journal) { _operationJournal = journal; return this; }

    public ATMController Build() {
        ValidateDependencies();
        return new ATMController(_cardReader!, _cashDispenser!, _cashAcceptor!, _bankingService!, _receiptPrinter!, _operationJournal!);
    }

    private void ValidateDependencies() {
        var fields = this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var field in fields)
        {
            if (field.GetValue(this) is null)
            {
                throw new InvalidOperationException($"Зависимость не была предоставлена: {field.Name}");
            }
        }
    }
}

