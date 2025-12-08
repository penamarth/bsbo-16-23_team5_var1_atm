using System;
using ATM.Kernel.Common;
using ATM.Contexts.Operation;
using ATM.Kernel.Hardware;
using ATM.Contexts.Banking;
using ATM.Kernel.Storage;
using ATM.Kernel.Models;
using ATM.Contexts.Maintenance;

Logger.Log("Запуск системы...");

var storage = new LocalStorage();
var operationJournal = new OperationJournal(storage);

var cardReader = new CardReader();
var cashDispenser = new CashDispenser(initialCash: 50000m);
var cashAcceptor = new CashAcceptor();
var receiptPrinter = new ReceiptPrinter();
var bankingService = new BankingServiceClient(storage);
var maintenanceController = new MaintenanceController(cashDispenser, cashAcceptor);

var controller = new ATMControllerBuilder()
    .WithCardReader(cardReader)
    .WithCashDispenser(cashDispenser)
    .WithCashAcceptor(cashAcceptor)
    .WithBankingService(bankingService)
    .WithReceiptPrinter(receiptPrinter)
    .WithOperationJournal(operationJournal)
    .Build();

controller.Setup();

var card = new CardData("1234-5678-9012-3456");
var pin = new Pin("1234");

var session = controller.StartSession(card, pin);
if (session is not null)
{
    var activeSession = session.Value;
    
    // Прецедент: Проверить баланс
    controller.CheckBalance(activeSession);

    // Прецедент: Внесение наличных
    controller.Deposit(activeSession, 5000m);

    // Прецедент: Снять наличные
    controller.Withdraw(activeSession, 3000m);

    // Прецедент: Перевод средств
    controller.Transfer(activeSession, "9876-5432-1098-7654", 1200m);

    // Прецедент: Смена PIN-кода
    controller.ChangePin(activeSession.Card, pin, new Pin("4321"));

    // Повторная проверка баланса после операций
    controller.CheckBalance(activeSession);

    // Завершение сессии
    controller.EndSession(activeSession.Card.CardNumber);
}

// Прецедент: Техническое обслуживание (пример)
var serviceCode = "9999";
if (maintenanceController.AuthenticateCollector(serviceCode))
{
    maintenanceController.RefillDispenser(10_000m);
    maintenanceController.CollectAcceptor();
    maintenanceController.CheckStatus();
}

controller.Shutdown();