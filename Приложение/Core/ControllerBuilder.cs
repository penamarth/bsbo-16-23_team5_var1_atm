using System.Reflection;
using ATM.Hardware;
using ATM.Services.Api;
using ATM.UI;

namespace ATM.Core;

public class ATMControllerBuilder {
    private Screen? _screen;
    private Keypad? _keypad;
    private CardReader? _cardReader;
    private CashDispenser? _cashDispenser;
    private IBankingService? _bankingService;
    private MenuHandler? _menuHandler;
    private ReceiptPrinter? _receiptPrinter;

    public ATMControllerBuilder WithScreen(Screen screen) { _screen = screen; return this; }
    public ATMControllerBuilder WithKeypad(Keypad keypad) { _keypad = keypad; return this; }
    public ATMControllerBuilder WithCardReader(CardReader reader) { _cardReader = reader; return this; }
    public ATMControllerBuilder WithCashDispenser(CashDispenser dispenser) { _cashDispenser = dispenser; return this; }
    public ATMControllerBuilder WithBankingService(IBankingService service) { _bankingService = service; return this; }
    public ATMControllerBuilder WithMenuHandler(MenuHandler menuHandler) { _menuHandler = menuHandler; return this; }
    public ATMControllerBuilder WithReceiptPrinter(ReceiptPrinter printer) { _receiptPrinter = printer; return this; }

    public ATMController Build() {
        ValidateDependencies();
        return new ATMController(_screen!, _keypad!, _cardReader!, _cashDispenser!, _bankingService!, _menuHandler!, _receiptPrinter!);
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