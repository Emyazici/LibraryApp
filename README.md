# LibraryApp

A library management system built with **.NET 8** following **Clean Architecture**, **Domain-Driven Design (DDD)**, and **CQRS** principles.

---

## Architecture

```
LibraryApp/
├── LibraryApp.Domain/          # Business rules and entities — no external dependencies
├── LibraryApp.Application/     # Use cases (CQRS handlers, pipeline behaviors)
└── LibraryApp.Tests/           # Unit tests (Domain + Application)
```

Layers depend inward: Domain knows nothing about other layers; Application knows only Domain.

---

## Tech Stack

| Package | Purpose |
|---|---|
| .NET 8 | Runtime |
| MediatR | CQRS — command/query dispatching |
| FluentValidation | Request validation via pipeline behavior |
| xUnit + FluentAssertions | Unit testing |
| Moq | Mocking for Application layer tests |

---

## Domain Layer

### Entities

**Book** — Book aggregate. Manages stock and loan status.

- `Borrow()` → decrements stock; sets status to `Loaned` when the last copy is taken
- `Return()` → increments stock; restores `Available` status

**Loan** — Loan aggregate. Encapsulates all return business rules.

- `Return()` → on-time return → `Returned`; late return → `Overdue` (late fee calculated)
- `CalculateFee()` → 2 TRY per overdue day

**Member** / **Author** — Member and author entities.

### Value Objects

| VO | Responsibility |
|---|---|
| `ISBN` | ISBN-10 / ISBN-13 format validation, strips hyphens and spaces |
| `LoanPeriod` | Start/end date pair, `IsOverdue()`, `Extend()`, enforces 60-day max |
| `Money` | Negative guard, same-currency addition, immutable |

### Domain Events

`LoanBorrowedEvent` · `LoanReturnedEvent` · `BookCreatedEvent` · `BookBorrowedEvent` · `BookReturnedEvent`

---

## Application Layer

### Commands

| Command | What it does |
|---|---|
| `AddBookCommand` | Adds a new book; checks for duplicate ISBN |
| `BorrowBookCommand` | Issues a loan; validates stock, active loans, and member existence |
| `ReturnBookCommand` | Processes a return; checks ownership and calculates late fee |

### Queries

| Query | What it does |
| --- | --- |
| `GetBookByIdQuery` | Returns book details as a DTO |
| `GetActiveLoansByMemberQuery` | Lists the authenticated member's active loans (N+1-free) |

### Pipeline Behavior Order

```
LoggingBehavior → ValidationBehavior → PerformanceBehavior → Handler
```

Every request is automatically logged, validated via FluentValidation, and flagged if it exceeds 500 ms.

---

## Tests

```bash
dotnet test
```

### Summary

| Suite | Tests | Status |
| --- | --- | --- |
| `LoanTests` | 16 | ✅ All passed |
| `BookTests` | 14 | ✅ All passed |
| `MoneyTests` | 12 | ✅ All passed |
| `ISBNTests` | 11 | ✅ All passed |
| `LoanPeriodTests` | 14 | ✅ All passed |
| `BorrowBookCommandHandlerTests` | 8 | ✅ All passed |
| `ReturnBookCommandHandlerTests` | 7 | ✅ All passed |
| `AddBookCommandHandlerTests` | 6 | ✅ All passed |
| `GetActiveLoansByMemberQueryHandlerTests` | 8 | ✅ All passed |
| **Total** | **95** | **✅ 95 / 95** |

> Run time: ~0.9 s · Framework: xUnit + FluentAssertions + Moq

---

### Domain Tests

#### LoanTests — 16 tests

| # | Test | What it verifies |
| --- | --- | --- |
| 1 | `Create_WithValidData_SetsActiveStatus` | New loan starts as Active |
| 2 | `Create_WithValidData_FeeIsZero` | Initial fee is zero |
| 3 | `Create_WithValidData_RaisesBorrowedEvent` | LoanBorrowedEvent is raised |
| 4 | `Create_WithEmptyBookId_ThrowsBusinessRuleException` | Empty BookId rejected |
| 5 | `Create_WithEmptyMemberId_ThrowsBusinessRuleException` | Empty MemberId rejected |
| 6 | `Return_WhenOnTime_SetsReturnedStatus` | On-time return → Returned |
| 7 | `Return_WhenOnTime_SetsActualReturnDate` | ActualReturnDate is set |
| 8 | `Return_WhenOnTime_FeeRemainsZero` | No fee for on-time return |
| 9 | `Return_WhenOverdue_SetsOverdueStatus` | Late return → Overdue |
| 10 | `Return_WhenOverdue_SetsPositiveFee` | Fee is positive when late |
| 11 | `Return_WhenOverdue_SetsPositiveOverDueDays` | OverDueDays is positive |
| 12 | `Return_RaisesReturnedEvent` | LoanReturnedEvent is raised |
| 13 | `Return_WhenAlreadyReturned_ThrowsBusinessRuleException` | Double return rejected |
| 14 | `Return_WhenAlreadyOverdue_ThrowsBusinessRuleException` | Overdue loan cannot be returned again |
| 15 | `CalculateFee_WhenNotOverdue_ReturnsZero` | No fee before due date |
| 16 | `CalculateFee_WhenOverdue_ReturnsPositive` | Fee > 0 after due date |

#### BookTests — 14 tests

| # | Test | What it verifies |
| --- | --- | --- |
| 1 | `Create_WithValidData_SetsAvailableStatus` | New book is Available |
| 2 | `Create_WithValidData_SetsCorrectStock` | Stock matches input |
| 3 | `Create_WithEmptyTitle_ThrowsBusinessRuleException` | Empty title rejected |
| 4 | `Create_WithEmptyAuthorId_ThrowsBusinessRuleException` | Empty AuthorId rejected |
| 5 | `Create_WithZeroStock_ThrowsBusinessRuleException` | Zero stock rejected |
| 6 | `Create_WithNegativeStock_ThrowsBusinessRuleException` | Negative stock rejected |
| 7 | `Borrow_DecreasesStockByOne` | Stock decrements on borrow |
| 8 | `Borrow_WhenNotLastCopy_KeepsAvailableStatus` | Available with remaining stock |
| 9 | `Borrow_WhenLastCopy_SetsLoanedStatus` | Loaned when stock hits 0 |
| 10 | `Borrow_WhenLoaned_ThrowsBusinessRuleException` | No stock → borrow rejected |
| 11 | `Borrow_RaisesBorrowedEvent` | BookBorrowedEvent is raised |
| 12 | `Return_IncreasesStockByOne` | Stock increments on return |
| 13 | `Return_SetsAvailableStatus` | Status resets to Available |
| 14 | `Return_RaisesReturnedEvent` | BookReturnedEvent is raised |

#### MoneyTests — 12 tests

| # | Test | What it verifies |
| --- | --- | --- |
| 1 | `Create_WithValidData_SetsAmountAndCurrency` | Amount and currency stored correctly |
| 2 | `Create_CurrencyNormalizesToUpperCase` | `"try"` → `"TRY"` |
| 3 | `Create_WithZeroAmount_Succeeds` | Zero is a valid amount |
| 4 | `Create_WithNegativeAmount_ThrowsBusinessRuleException` | Negative amount rejected |
| 5 | `Create_WithEmptyCurrency_ThrowsBusinessRuleException` | Empty currency rejected |
| 6 | `Create_WithWhitespaceCurrency_ThrowsBusinessRuleException` | Whitespace currency rejected |
| 7 | `Add_SameCurrency_ReturnsSummedAmount` | 10 + 5 = 15 TRY |
| 8 | `Add_DifferentCurrency_ThrowsBusinessRuleException` | TRY + USD rejected |
| 9 | `Add_ReturnsNewInstance` | Immutability — new object returned |
| 10 | `Equality_SameAmountAndCurrency_AreEqual` | Value object equality |
| 11 | `Equality_DifferentAmount_AreNotEqual` | Different amounts not equal |
| 12 | `Equality_DifferentCurrency_AreNotEqual` | Different currencies not equal |

#### ISBNTests — 11 tests

| # | Test | What it verifies |
| --- | --- | --- |
| 1 | `Create_ValidISBN13_ReturnsCleanedValue` | 13-digit ISBN accepted |
| 2 | `Create_ValidISBN10_ReturnsCleanedValue` | 10-digit ISBN accepted |
| 3 | `Create_WithDashes_StripsAndAccepts` | Hyphens stripped automatically |
| 4 | `Create_WithSpaces_StripsAndAccepts` | Spaces stripped automatically |
| 5 | `Create_Empty_ThrowsBusinessRuleException` | Empty string rejected |
| 6 | `Create_Whitespace_ThrowsBusinessRuleException` | Whitespace rejected |
| 7 | `Create_WrongLength_ThrowsBusinessRuleException` | Wrong length rejected |
| 8 | `Create_NonDigitChars_ThrowsBusinessRuleException` | Letters rejected |
| 9 | `Equality_SameValue_AreEqual` | Same ISBN → equal |
| 10 | `Equality_DifferentValue_AreNotEqual` | Different ISBNs → not equal |
| 11 | `Equality_DashVsNoDash_AreEqual` | Dashed and plain form equal |

#### LoanPeriodTests — 14 tests

| # | Test | What it verifies |
| --- | --- | --- |
| 1 | `Create_ValidPeriod_SetsDatesCorrectly` | Dates stored correctly |
| 2 | `Create_DueBeforeStart_ThrowsBusinessRuleException` | Due < start rejected |
| 3 | `Create_DueEqualToStart_ThrowsBusinessRuleException` | Due = start rejected |
| 4 | `Create_Over60Days_ThrowsBusinessRuleException` | > 60 days rejected |
| 5 | `Create_Exactly60Days_Succeeds` | Exactly 60 days allowed |
| 6 | `IsOverdue_WhenExpiredYesterday_ReturnsTrue` | Past due date → overdue |
| 7 | `IsOverdue_WhenDueIsFuture_ReturnsFalse` | Future due date → not overdue |
| 8 | `DaysRemaining_WhenFuture_ReturnsPositive` | Positive when time remains |
| 9 | `DaysRemaining_WhenOverdue_ReturnsNegativeOrZero` | Negative/zero when past due |
| 10 | `Extend_AddsCorrectDays` | Extension adds correct days |
| 11 | `Extend_PreservesStartDate` | Start date unchanged after extend |
| 12 | `Extend_WhenExceeds60Days_ThrowsBusinessRuleException` | Extension over 60 days rejected |
| 13 | `Equality_SameDates_AreEqual` | Same dates → equal |
| 14 | `Equality_DifferentDue_AreNotEqual` | Different due → not equal |

---

### Application Tests

#### BorrowBookCommandHandlerTests — 8 tests

| # | Test | What it verifies |
| --- | --- | --- |
| 1 | `Handle_WhenBookNotFound_ReturnsFailure` | Missing book → failure result |
| 2 | `Handle_WhenMemberNotFound_ReturnsFailure` | Missing member → failure result |
| 3 | `Handle_WhenAlreadyHasActiveLoan_ReturnsFailure` | Duplicate active loan → failure result |
| 4 | `Handle_ValidRequest_ReturnsSuccessWithNonEmptyLoanId` | Happy path returns a valid Loan ID |
| 5 | `Handle_ValidRequest_CallsBookUpdateAsync` | Book repository updated once |
| 6 | `Handle_ValidRequest_CallsLoanAddAsync` | Loan persisted once |
| 7 | `Handle_ValidRequest_CallsSaveChangesOnce` | Unit of work committed once |
| 8 | `Handle_ValidRequest_BookStockDecreased` | Stock actually decremented |

#### ReturnBookCommandHandlerTests — 7 tests

| # | Test | What it verifies |
| --- | --- | --- |
| 1 | `Handle_WhenLoanNotFound_ReturnsFailure` | Missing loan → failure result |
| 2 | `Handle_WhenNotOwner_ReturnsFailure` | Wrong user → failure result |
| 3 | `Handle_WhenBookNotFound_ReturnsFailure` | Missing book → failure result |
| 4 | `Handle_ValidRequest_ReturnsSuccess` | Happy path returns success |
| 5 | `Handle_ValidRequest_CallsLoanUpdateAsync` | Loan repository updated once |
| 6 | `Handle_ValidRequest_CallsBookUpdateAsync` | Book repository updated once |
| 7 | `Handle_ValidRequest_CallsSaveChangesOnce` | Unit of work committed once |

#### AddBookCommandHandlerTests — 6 tests

| # | Test | What it verifies |
| --- | --- | --- |
| 1 | `Handle_WhenIsbnAlreadyExists_ReturnsFailure` | Duplicate ISBN → failure result |
| 2 | `Handle_WhenIsbnAlreadyExists_DoesNotCallAddAsync` | No persistence on duplicate |
| 3 | `Handle_ValidRequest_ReturnsSuccessWithNonEmptyBookId` | Happy path returns a valid Book ID |
| 4 | `Handle_ValidRequest_CallsAddAsync` | Book persisted once |
| 5 | `Handle_ValidRequest_CallsSaveChangesOnce` | Unit of work committed once |
| 6 | `Handle_ValidRequest_DoesNotCallSaveChangesWhenIsbnExists` | No commit on duplicate ISBN |

#### GetActiveLoansByMemberQueryHandlerTests — 8 tests

| # | Test | What it verifies |
| --- | --- | --- |
| 1 | `Handle_WhenNoLoans_ReturnsEmptyList` | Empty result for member with no loans |
| 2 | `Handle_WithLoans_ReturnsDtoPerLoan` | One DTO produced per loan |
| 3 | `Handle_WithLoans_DtoContainsCorrectBookTitle` | Book title mapped correctly |
| 4 | `Handle_WithLoans_DtoContainsCorrectLoanId` | Loan ID mapped correctly |
| 5 | `Handle_WhenBookMissing_DtoShowsBilinmiyor` | Missing book → "Bilinmiyor" fallback |
| 6 | `Handle_WithManyLoans_CallsGetByIdsAsyncOnce` | Batch load — no N+1 query |
| 7 | `Handle_WithManyLoans_CallsGetActiveLoansByMemberOnce` | Filtered query called once |
| 8 | `Handle_WithManyLoans_CallsGetActiveLoansByMemberOnce` | Member isolation verified |

---

## Running the Project

```bash
# Build
dotnet build

# Run all tests
dotnet test

# Domain tests only
dotnet test --filter "FullyQualifiedName~Domain"

# Application tests only
dotnet test --filter "FullyQualifiedName~Application"
```

> **Note:** The Infrastructure (DbContext, repository implementations) and API layers have not been implemented yet. The existing layers are fully testable in isolation without them.

---

## Business Rules

- A member cannot borrow the same book more than once at the same time.
- The maximum loan period is **60 days**.
- A book with no stock (`Loaned` status) cannot be borrowed.
- A loan that has already been returned or marked overdue cannot be returned again.
- Late fee: **2 TRY per overdue day**.
