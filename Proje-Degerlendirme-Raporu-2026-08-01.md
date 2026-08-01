# LibraryApp — Proje Yapısı İncelemesi ve Kalite Değerlendirmesi

> Tarih: 2026-08-01
> Kapsam: Tüm dosya/klasör yapısı, katman katman kod okuması, `dotnet build` + `dotnet test` doğrulaması.
> Not: Bu repoda daha önce de bir inceleme yapılmış (`Kod İnceleme Raporu — LibraryApp.txt`, 25 Nisan 2026, puan 65/100) ve orada bulunan 6 kritik hatanın hepsi bu incelemede **gerçekten düzeltilmiş** olarak doğrulandı. Bu rapor o incelemeyi tekrar etmiyor, mevcut durumu sıfırdan değerlendiriyor.

---

## 1. Genel Bakış

.NET 8 üzerinde Clean Architecture + DDD + CQRS (MediatR) ile yazılmış bir kütüphane yönetim sistemi. Şu an **4 proje** var, planlanan **6 projeden** 2'si (API, WebUI) henüz yok:

```
LibraryApp/
├── LibraryApp.Domain/          ✅ var — entity, VO, event, repository interface'leri
├── LibraryApp.Application/     ✅ var — CQRS command/query, pipeline behavior
├── LibraryApp.Infrastructure/  ✅ var — EF Core, Identity, JWT
├── LibraryApp.Tests/           ✅ var — 95 unit test (Domain + Application)
├── LibraryApp.API/             ❌ yok — planlandı (Faz 7.1), mobil için JWT
└── LibraryApp.WebUI/           ❌ yok — planlandı (Faz 7.2), tarayıcı için Cookie+MVC
```

Build: **temiz** (3 nullable uyarısı dışında hata yok). Test: **95/95 geçiyor**.

Bu, "bitmemiş ama dürüst" bir proje — plan dosyaları (`LibraryApp-Infrastructure-Task.md`, `-Ilerleme.md`) neyin yapıldığını neyin yapılmadığını gayet net anlatıyor. Aşağıdaki değerlendirme bu planlara ek olarak, koda bakarak bulduğum somut noktaları içeriyor.

---

## 2. Dosya Yapısı — Katman Katman

### 2.1 `LibraryApp.Domain/`
```
Common/          Entity, AggregateRoot, ValueObject, IDomainEvent
Entities/        Author, Book, Loan, Member
Enums/           BookStatus, LoanStatus
Events/          BookEvents.cs, LoanEvents.cs
Exceptions/      Domainexceptions.cs   ← dosya adı yazımı (bkz. §4)
Repositories/    IBookRepository, ILoanRepository, IMemberRepository, IUnitOfWork
ValueObjects/    ISBN, LoanPeriod, Money
```
Dış bağımlılığı **yok** (csproj'da hiçbir PackageReference yok) — Dependency Rule'a tam uyumlu, MediatR referansı da kaldırılmış (Faz 0 tamamlanmış).

**Eksik:** `IAuthorRepository` yok (bilinçli, Ilerleme.md'de not edilmiş — Author'ı tek başına kullanan bir Command/Query yok).

### 2.2 `LibraryApp.Application/`
```
Behaviors/            LoggingBehavior, PerformanceBehavior, ValidationBehaviors
Commands/             AddBook, BorrowBook, DeleteBook, DeleteMember, ReturnBook
Common/               DomainEventNotification, ICurrentUserService, IJwtTokenGenerator, Result
Queries/              GetActiveLoansByMember, GetBookById, GetLoanHistoryByMember
```
Her komut/sorgu kendi klasöründe Command/Query + Handler + (varsa) Validator üçlüsü olarak organize — tutarlı ve okunabilir.

**Eksik:** Auth akışı için hiçbir Command yok — `RegisterCommand`/`LoginCommand` planlanmış (Ilerleme.md, "Karar 9") ama hiç yazılmamış. `UpdateBookCommand`, `GetAllBooksQuery` gibi temel CRUD parçaları da yok (henüz Presentation olmadığı için doğal, ama Presentation yazılırken bunlar da gerekecek).

### 2.3 `LibraryApp.Infrastructure/`
```
Authentication/       JwtTokenGenerator
Identity/              ApplicationUser, ApplicationRole, IdentitySeeder
Persistence/
  Configurations/      Book, Loan, Member, Author, ApplicationUser
  Repositories/        Book, Loan, Member, UnitOfWork
  LibraryDbContext.cs
Services/              CurrentUserService
```
**Eksik — en kritik boşluk:** `Migrations/` klasörü **yok**. Yani DbContext ve tüm configuration'lar yazılmış ama hiç migration üretilmemiş, veritabanı hiç oluşturulmamış/doğrulanmamış. Value object `OwnsOne` mapping'leri (private constructor'lı ISBN/Money/LoanPeriod) ilk migration'da genelde sorun çıkarır — bu henüz test edilmemiş bir risk.

`appsettings.json` da yok (normal, çünkü onu barındıracak bir Presentation projesi henüz yok), yani JWT SecretKey/connection string şu an hiçbir yerde tanımlı değil.

### 2.4 `LibraryApp.Tests/`
```
Application/    AddBook, BorrowBook, ReturnBook, GetActiveLoansByMember handler testleri
Domain/         BookTests, LoanTests, ValueObjects/ISBN, LoanPeriod, Money testleri
UnitTest1.cs    ← boş, "artık kullanılmıyor" yorumu var (bkz. §4)
```
95 test, hepsi geçiyor. Domain ve Application katmanı iyi kapsanmış.

**Eksik:** Infrastructure katmanı için **hiç test yok** — repository implementasyonları, `SaveChangesAsync` override'ındaki reflection tabanlı domain event dispatch, EF configuration'lar (`OwnsOne` mapping'leri) test edilmemiş. Bunlar migration/gerçek DB olmadan da EF Core InMemory ya da SQLite ile test edilebilirdi.

### 2.5 Repo kökü
```
LibraryApp.sln
README.md                                    — çok kapsamlı, test tablosu güncel
Kod İnceleme Raporu — LibraryApp.txt         — önceki inceleme (gitignore'da, versiyonlanmıyor)
LibraryApp-Infrastructure-Task.md            — plan (gitignore'da)
LibraryApp-Infrastructure-Ilerleme.md        — ilerleme günlüğü (gitignore'da DEĞİL — tutarsızlık)
libraryapp-issues/*.md (9 dosya)             — GitHub issue taslakları + create-issues.sh (gitignore'da)
image.png, image-1.png                       — kullanım amacı belirsiz, muhtemelen README ekran görüntüsü kalıntısı
```

---

## 3. Bulgular

### 3.1 Kritik / Doğruluk Riski

**B1 — `AddBookCommandValidator` ISBN regex'i domain kuralıyla çelişiyor** ✅ DÜZELTİLDİ
`LibraryApp.Application/Commands/AddBook/AddBookCommandValidator.cs:21`
```csharp
.Matches(@"^\d{3}-\d{10}$")   // sadece "978-1234567890" formatını kabul eder
```
Ama domain'deki `ISBN.Create()` (`LibraryApp.Domain/ValueObjects/ISBN.cs`) tireleri/boşlukları temizleyip **10 veya 13 haneli** her sayıyı kabul ediyor — tiresiz de, 10 haneli de geçerli. Sonuç: geçerli bir ISBN'in çoğu hâli (`9781234567890`, `0134685997` gibi) validator'da **reddediliyor**, komut hiç handler'a ulaşmıyor. Bu iki katman birbirinden habersiz büyümüş; validator handler'dan önce çalıştığı için domain kuralı fiilen erişilemez durumda.

**B2 — `Member.Balance` public setter ile dışarıya açık** ✅ DÜZELTİLDİ
`LibraryApp.Domain/Entities/Member.cs:12`
```csharp
public Money Balance { get; set; }
```
Aynı sınıftaki diğer her alan `private set`; `Loan.Fee` için de aynı sorun daha önce tespit edilip düzeltilmişti (önceki rapor, Hata 4). `Balance` o düzeltmeden kaçmış — herhangi bir kod `member.Balance = ...` diyerek iş kuralı geçmeden bakiyeyi değiştirebilir. Şu an hiçbir yerde kullanılmıyor (grep ile doğrulandı), yani aktif bir hataya yol açmıyor ama DDD ihlali ve gelecekte sessiz bir hataya davetiye.

### 3.2 Orta Öncelik

**B3 — Migration hiç üretilmemiş.** `Migrations/` klasörü yok. Value object `OwnsOne` mapping'leri (özellikle private constructor + parametre adı eşleşmesiyle uğraşılan `LoanPeriod`) ilk migration'da sorun çıkarabilir — bu hâlâ doğrulanmamış bir varsayım.

**B4 — Auth uçtan uca çalışmıyor.** Identity, JWT üretimi, `CurrentUserService`, rol seed'i — hepsi hazır ama `RegisterCommand`/`LoginCommand` yok. Yani bir kullanıcı oluşturup giriş yapabileceğiniz hiçbir yol yok; `ICurrentUserService.UserId` hiçbir zaman gerçek bir JWT claim'inden dolmayacak çünkü token üretecek endpoint yok.

**B5 — Infrastructure katmanı test edilmemiş.** Repository'ler, `SaveChangesAsync` override'ı (reflection + `MakeGenericType`/`Activator.CreateInstance` — hataya en açık kod parçalarından biri), EF configuration'lar hiç test edilmemiş.

**B6 — Paket sürüm tutarsızlığı.** `LibraryApp.Application.csproj`'da `Microsoft.Extensions.DependencyInjection.Abstractions` ve `Microsoft.Extensions.Logging.Abstractions` **10.0.7** sürümünde, ama proje `net8.0` hedefliyor ve Infrastructure/diğer paketler 8.0.x. Build şu an geçiyor ama bu majör sürüm sıçraması (8→10) bilinçli bir seçim gibi görünmüyor; ileride bir yükseltme sırasında sürpriz bağımlılık çakışmasına yol açabilir.

**B7 — Namespace/klasör uyuşmazlığı.** `Behaviors/LoggingBehavior.cs` (ve diğer ikisi) fiziksel olarak `Application/Behaviors/` altında ama namespace `LibraryApp.Application.Common.Behaviors`. Küçük ama okunabilirliği düşüren bir tutarsızlık.

**B8 — `Book.Money` adlandırması kafa karıştırıcı.** Property adı `Money`, tipi de `Money` (`public Money Money { get; private set; }`). `Price` gibi bir isim niyeti çok daha net anlatırdı; DTO'larda zaten `Price` deniyor (`BookDto.Price`), yani isimlendirme entity ile DTO arasında bile tutarsız.

### 3.3 Düşük Öncelik / Kozmetik

- `LibraryApp.Tests/UnitTest1.cs` — içi boş, "artık kullanılmıyor" yorumu olan template dosyası. Silinmesi gerekiyor.
- `LibraryApp.Domain/Exceptions/Domainexceptions.cs` — dosya adı `Domainexceptions.cs` (küçük "e"), önceki raporda `Enitiy.cs` yazım hatası düzeltilmiş ama bu dosya adı kalmış.
- 3 nullable uyarısı (`Member.Balance`, `ApplicationUser.Name`, `ApplicationUser.Surname`) — zararsız ama `Nullable` açıkken temiz build hedefleniyorsa giderilmeli.
- `image.png`, `image-1.png` — repo kökünde amaçları belirsiz iki görsel.
- Dokümantasyon dağınık: `Kod İnceleme Raporu…txt`, `LibraryApp-Infrastructure-Task.md`, `-Ilerleme.md`, `libraryapp-issues/*.md` — hepsi değerli ama çoğu `.gitignore`'da (ekip ile paylaşılmıyor, sadece yerel). `-Ilerleme.md` ise unutulmuş gibi gitignore dışında kalmış — tutarsız.
- CI/CD yok (GitHub Actions vb.) — `dotnet build`/`dotnet test` her PR'da otomatik çalışmıyor.

---

## 4. Kalite Değerlendirmesi (Kategori Bazlı)

| Katman / Alan | Puan | Not |
|---|---|---|
| Domain tasarımı | 9/10 | VO'lar, factory+private ctor, aggregate encapsulation sağlam. Balance setter'ı tek leke. |
| Application/CQRS | 8/10 | Pipeline sırası doğru, Result pattern temiz, N+1 çözülmüş. ISBN validator/domain çelişkisi (B1) gerçek bir kullanılabilirlik hatası. |
| Infrastructure | 6/10 | Kod kalitesi iyi (soft-delete, query filter, UoW deseni tutarlı) ama migration hiç çalıştırılmamış, hiç test yok — "yazıldı ama doğrulanmadı" aşaması. |
| Test kapsamı | 7/10 | Domain+Application için 95 test iyi durumda; Infrastructure ve Auth akışı sıfır. |
| Presentation | 0/10 | Henüz yok (bilinçli, plana göre sırada). |
| DevOps/Docs | 5/10 | README mükemmel ama repo kökü dağınık, CI yok, plan dosyaları gitignore'da tutarsız. |

**Genel puan: 68/100** — önceki incelemenin 65/100'ünden biraz yukarıda. Kritik veri kaybı hataları gerçekten kapatılmış; kalan puan kaybı çoğunlukla "henüz yapılmadı" (migration, auth uçtan uca, Presentation, Infra testleri) kaleminden geliyor, yeni "kırık" bir şey az (B1, B2).

---

## 5. Önerilen Sıra

1. **B1 ve B2'yi düzelt** — ikisi de 5 dakikalık değişiklik, biri kullanılabilirlik hatası biri DDD ihlali. ✅ DÜZELTİLDİ
2. **Migration'ı çalıştır** (Faz 6) — `OwnsOne` mapping'lerinin gerçekten çalıştığını doğrulamadan ilerlemek risk biriktiriyor.
3. **`RegisterCommand`/`LoginCommand` yaz** — Identity/JWT altyapısı hazır, kullanılamıyor olması en büyük eksik.
4. Infrastructure için birkaç entegrasyon testi (EF Core InMemory/SQLite ile) — özellikle `SaveChangesAsync` reflection kısmı.
5. `LibraryApp.API` (Faz 7.1) ile ilk uçtan uca akışı kur.
