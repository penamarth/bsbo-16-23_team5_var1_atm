# Прецедент: Внесение наличных

## Рамки
Бекенд банкомата NextGen: консольное приложение, локальное хранилище JSON (`storage/`), без сетевого вызова внешнего банка.

## Уровень
User Goal.

## Основной исполнитель
Клиент (активная сессия банкомата).

## Заинтересованные лица
- **Клиент.** Пополнить счет и получить подтверждение.
- **Банк/оператор.** Фиксировать каждое зачисление и остаток купюроприемника.
- **Инкассация.** Знать накопленную сумму в купюроприемнике для забора.

## Предусловия
- Сессия активна: `AtmSession` получен через `StartSession(card, pin)`.
- Купюроприемник исправен.
- Локальное хранилище доступно для записи.

## Постусловия
- **Успех:** баланс счета увеличен, сумма учтена во внутреннем счетчике `CashAcceptor`, запись `DEPOSIT` сохранена в `storage/operations.json`.
- **Отказ:** баланс не меняется, при необходимости средства возвращены (`EjectCash`), в журнале запись `Failed`.
- Чек печатается только если вызван `ReceiptPrinter` внешним сценарием.

## Логика банкомата (бекенд)
- `CashAcceptor.AcceptCash(amount?)` суммирует внесенную сумму во внутреннем счётчике.
- `ATMController.Deposit(session, amount, cashAlreadyAccepted)`:
  - проверяет `amount > 0`;
  - при `!cashAlreadyAccepted` вызывает `AcceptCash(amount)`;
  - вызывает `BankingServiceClient.Deposit` (увеличивает баланс в JSON-файле счетов);
  - при отказе выполняет `CashAcceptor.EjectCash()`;
  - логирует результат через `OperationJournal.Add` (маска карты, сумма, статус).
- `OperationJournal` хранится в `storage/operations.json` через `LocalStorage`.

## Основной успешный сценарий (backend)
1. Внешний сценарий принимает/знает сумму `amount` (можно через `CashAcceptor.AcceptCash()`).
2. Вызывает `ATMController.Deposit(session, amount, cashAlreadyAccepted:true|false)`.
3. `Deposit` валидирует сумму и при необходимости принимает купюры.
4. `BankingServiceClient.Deposit` увеличивает баланс и сохраняет данные (accounts/bindings/pins/withdrawals).
5. `OperationJournal.Add` пишет `DEPOSIT` со статусом `Success`.
6. По желанию сценарий печатает чек через `ReceiptPrinter.PrintReceipt`.

## Альтернативы
- `amount <= 0` → сразу `OperationStatus.Failed`, возвращается `false`.
- `BankingServiceClient.Deposit` вернул `false` (некорректная сумма) → `EjectCash`, журнал `Failed`.
- Сценарий отменяет операцию после приёма наличных → вызывает `EjectCash`, журнал по решению сценария.

## Системные операции (факт кода)
- `CashAcceptor.AcceptCash([decimal]) : decimal` — накапливает сумму, возвращает принятое.
- `ATMController.Deposit(session, amount, cashAlreadyAccepted=false) : bool` — оркестрация, журналирование.
- `BankingServiceClient.Deposit(accountId, amount) : bool` — прибавляет баланс в JSON и сохраняет.
- `CashAcceptor.EjectCash()` — обнуляет счетчик внесённых средств.
- `ReceiptPrinter.PrintReceipt(text)` — выводит строку в лог (чек опционален, UI нет).

## Специальные требования (реализованные)
- Номер карты в журнале маскируется.
- Все данные (балансы, привязки карт, PIN, журнал) лежат в `AppContext.BaseDirectory/storage`.
- Идемпотентность не гарантируется: повторный вызов `Deposit` с той же суммой повторно увеличит баланс.

## Частота использования
Средняя для учебного стенда; зависит от вызывающих сценариев.

## Открытые вопросы
- Нужен ли верхний лимит суммы для одной операции?
- Следует ли печатать чек автоматически после успеха?
- Нужно ли фиксировать остаток купюроприемника в журнале операций?


