using System;
using ATM.Kernel.Common;
using ATM.Contexts.Operation;
using ATM.Kernel.Hardware;
using ATM.Contexts.Banking;
using ATM.Kernel.UI;

Logger.Log("Запуск системы...");

var screen = new Screen();
var keypad = new Keypad();
var cardReader = new CardReader();
var cashDispenser = new CashDispenser(initialCash: 50000m);
var cashAcceptor = new CashAcceptor();
var receiptPrinter = new ReceiptPrinter();
var bankingService = new BankingServiceClient();
var menuHandler = new MenuHandler(screen, keypad);

var controller = new ATMControllerBuilder()
    .WithScreen(screen)
    .WithKeypad(keypad)
    .WithCardReader(cardReader)
    .WithCashDispenser(cashDispenser)
    .WithCashAcceptor(cashAcceptor)
    .WithBankingService(bankingService)
    .WithMenuHandler(menuHandler)
    .WithReceiptPrinter(receiptPrinter)
    .Build();

controller.Setup();
controller.Run();
controller.Shutdown();