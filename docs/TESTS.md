# Test Catalogue

Detailed list of the unit tests (`LibraryApp.Tests`) and integration tests (`LibraryApp.IntegrationTests`). See the [README](../README.md#tests) for the summary and how to run them.

> Run with `dotnet test` (integration tests need Docker) · xUnit + FluentAssertions + Moq + Testcontainers

---

## Domain Tests

### LoanTests — 16 tests

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

### BookTests — 14 tests

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

### MoneyTests — 12 tests

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

### ISBNTests — 11 tests

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

### LoanPeriodTests — 14 tests

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

## Application Tests

### BorrowBookCommandHandlerTests — 8 tests

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

### ReturnBookCommandHandlerTests — 7 tests

| # | Test | What it verifies |
| --- | --- | --- |
| 1 | `Handle_WhenLoanNotFound_ReturnsFailure` | Missing loan → failure result |
| 2 | `Handle_WhenNotOwner_ReturnsFailure` | Wrong user → failure result |
| 3 | `Handle_WhenBookNotFound_ReturnsFailure` | Missing book → failure result |
| 4 | `Handle_ValidRequest_ReturnsSuccess` | Happy path returns success |
| 5 | `Handle_ValidRequest_CallsLoanUpdateAsync` | Loan repository updated once |
| 6 | `Handle_ValidRequest_CallsBookUpdateAsync` | Book repository updated once |
| 7 | `Handle_ValidRequest_CallsSaveChangesOnce` | Unit of work committed once |

### AddBookCommandHandlerTests — 6 tests

| # | Test | What it verifies |
| --- | --- | --- |
| 1 | `Handle_WhenIsbnAlreadyExists_ReturnsFailure` | Duplicate ISBN → failure result |
| 2 | `Handle_WhenIsbnAlreadyExists_DoesNotCallAddAsync` | No persistence on duplicate |
| 3 | `Handle_ValidRequest_ReturnsSuccessWithNonEmptyBookId` | Happy path returns a valid Book ID |
| 4 | `Handle_ValidRequest_CallsAddAsync` | Book persisted once |
| 5 | `Handle_ValidRequest_CallsSaveChangesOnce` | Unit of work committed once |
| 6 | `Handle_ValidRequest_DoesNotCallSaveChangesWhenIsbnExists` | No commit on duplicate ISBN |

### GetActiveLoansByMemberQueryHandlerTests — 8 tests

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

### LoginCommandHandlerTests — 2 tests

| # | Test | What it verifies |
| --- | --- | --- |
| 1 | `Handle_ValidCredentials_ReturnsTokenFromService` | Token from `IAuthTokenService` is returned |
| 2 | `Handle_InvalidCredentials_ReturnsFailure` | Rejected credentials → failure result |

### RegisterCommandHandlerTests — 3 tests

| # | Test | What it verifies |
| --- | --- | --- |
| 1 | `Handle_WhenEmailAlreadyExists_ReturnsFailureAndDoesNotCallKeycloak` | Duplicate email short-circuits before Keycloak |
| 2 | `Handle_WhenKeycloakFails_ReturnsFailureAndDoesNotAddMember` | No orphan Member when Keycloak fails |
| 3 | `Handle_ValidRequest_AddsMemberWithKeycloakIdAndSavesOnce` | `Member.Id` = Keycloak `sub`, committed once |

### GetBooksQueryHandlerTests — 2 tests

| # | Test | What it verifies |
| --- | --- | --- |
| 1 | `Handle_WhenNoBooks_ReturnsEmptyList` | Empty catalogue → empty list |
| 2 | `Handle_WithBooks_MapsEachBookToDto` | Aggregate → DTO mapping |

---

## Integration Tests (`LibraryApp.IntegrationTests`)

Run against the real ASP.NET Core pipeline (`WebApplicationFactory<Program>`) and a throw-away PostgreSQL container (Testcontainers). Keycloak is replaced by a header-driven test authentication scheme and fakes for `IKeycloakAdminClient` / `IAuthTokenService`, so the tests need Docker but no running Keycloak.

| Suite | Test | What it verifies |
| --- | --- | --- |
| `HealthEndpointTests` | `Health_IsAnonymous_AndReportsHealthyDatabase` | `/health` is public and the DB check passes |
| `AuthEndpointsTests` | `Login_WithValidCredentials_ReturnsToken` | Login returns an `AuthTokenDto` |
| | `Login_WithInvalidCredentials_Returns400ProblemDetails` | Bad credentials → `problem+json` 400 |
| | `Register_WithInvalidEmailAndEmptyPassword_Returns400WithValidationErrors` | FluentValidation → 400 with field errors |
| `AuthorizationTests` | `ActiveLoans_WithoutToken_Returns401` | No token → 401 |
| | `DeleteBook_AsCustomer_Returns403_AndBookStillExists` | Wrong role → 403, nothing deleted |
| | `DeleteBook_AsAdmin_Returns204_ThenBookIsNotFound` | Admin soft-delete works |
| `BooksEndpointsTests` | `ListBooks_AsAuthenticatedUser_ContainsSeededBook` | `GET /api/books` lists the catalogue |
| `LoanFlowTests` | `RegisteredCustomer_CanBorrowAndReturnBook_AndSeesItInHistory` | Register → borrow → active → return → history |
| | `Borrow_AsEmployee_Returns403` | Borrowing is Customer-only |
