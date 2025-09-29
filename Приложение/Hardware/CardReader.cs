using ATM.Common;
using ATM.Models;

namespace ATM.Hardware;

public class CardReader {
    public CardData ReadCard() {
        Logger.Log("Чтение данных карты...");
        Thread.Sleep(654);
        return new CardData("1234-5678-9012-3456");
    }
}