namespace ATM.UI;

public class MenuHandler {
    private readonly Screen _screen;
    private readonly Keypad _keypad;

    public MenuHandler(Screen screen, Keypad keypad) {
        _screen = screen;
        _keypad = keypad;
    }

    public UserAction GetUserActionChoice() {
        _screen.DisplayMessage("\nВыберите операцию:");
        _screen.DisplayMessage("1. Проверить баланс");
        _screen.DisplayMessage("2. Снять наличные");
        _screen.DisplayMessage("3. Завершить работу");

        string choice = _keypad.GetInput();

        return choice switch {
            "1" => UserAction.CheckBalance,
            "2" => UserAction.Withdraw,
            "3" => UserAction.Exit,
            _ => UserAction.Unknown,
        };
    }
}