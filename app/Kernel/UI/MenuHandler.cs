namespace ATM.Kernel.UI;

public class MenuHandler {
    private readonly Screen _screen;
    private readonly Keypad _keypad;

    public MenuHandler(Screen screen, Keypad keypad) {
        _screen = screen;
        _keypad = keypad;
    }

    public UserAction GetUserActionChoice() {
        _screen.DisplayMessage("\n=== ГЛАВНОЕ МЕНЮ ===");
        _screen.DisplayMessage("1. Проверить баланс");
        _screen.DisplayMessage("2. Снять наличные");
        _screen.DisplayMessage("3. Сменить PIN");
        _screen.DisplayMessage("4. Внести наличные");
        _screen.DisplayMessage("5. Перевод");
        _screen.DisplayMessage("6. Выход");
        _screen.DisplayMessage("Выберите действие:");

        string? choice = _keypad.GetInput();
        
        return choice switch {
            "1" => UserAction.CheckBalance,
            "2" => UserAction.Withdraw,
            "3" => UserAction.ChangePin,
            "4" => UserAction.Deposit,
            "5" => UserAction.Transfer,
            "6" => UserAction.Exit,
            _ => UserAction.Unknown
        };
    }
}

