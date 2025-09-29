using System;
using ATM.Common;
using ATM.Core;
using ATM.Hardware;
using ATM.Services.Impl;
using ATM.UI;

Logger.Log("Запуск системы...");

var screen = new Screen();
var keypad = new Keypad();
var cardReader = new CardReader();
var cashDispenser = new CashDispenser(initialCash: 50000m);
var receiptPrinter = new ReceiptPrinter();
var bankingService = new BankingServiceClient();
var menuHandler = new MenuHandler(screen, keypad);

var controller = new ATMControllerBuilder()
    .WithScreen(screen)
    .WithKeypad(keypad)
    .WithCardReader(cardReader)
    .WithCashDispenser(cashDispenser)
    .WithBankingService(bankingService)
    .WithMenuHandler(menuHandler)
    .WithReceiptPrinter(receiptPrinter)
    .Build();

controller.Setup();
controller.Run();
controller.Shutdown();