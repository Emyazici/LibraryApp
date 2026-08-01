# LibraryApp — Infrastructure Katmanı İlerleme Durumu

> Bu dosya, `LibraryApp-Infrastructure-Task.md`'deki plana göre şu ana kadar ne yapıldığını, hangi kararların alındığını ve nasıl çalıştığımızı özetler. Yeni bir konuşmaya bununla başlanabilir.

---

## Tamamlanan Fazlar

### Faz 0 — Ön Temizlik ✅
- `IDomainEvent`, `INotification`'dan bağımsız (Domain'de MediatR referansı yok).
- `Application/Common/DomainEventNotification.cs` adaptörü var (`INotification` sarmalayıcı).

### Faz 1 — Proje İskeleti ✅
- `LibraryApp.Infrastructure` projesi oluşturuldu, `Domain` ve `Application`'a referans veriyor.
- DB provider olarak **Npgsql (PostgreSQL)** seçildi (SQL Server değil) — `LibraryApp.Infrastructure.csproj`'da `Npgsql.EntityFrameworkCore.PostgreSQL` paketi var.
- Identity/JWT paketleri de baştan eklendi (Faz 4 için hazır).

### Faz 2 — Persistence (EF Core) ✅
- **`Persistence/LibraryDbContext.cs`**: artık `IdentityDbContext<ApplicationUser, ApplicationRole, Guid>`'den türüyor (Faz 4.2'de dönüştürüldü). `DbSet<Book/Loan/Member/Author>` var. Constructor `DbContextOptions<LibraryDbContext>` + `IMediator` alıyor.
- **`Persistence/Configurations/`**: `BookConfiguration`, `LoanConfiguration`, `MemberConfiguration`, `AuthorConfiguration`, **`ApplicationUserConfiguration`** (yeni, Faz 4.2) — hepsi tamam.
- **`SaveChangesAsync` override edildi** (domain event dispatch): save öncesi event'i olan `AggregateRoot`'ları topla → `base.SaveChangesAsync` → başarılıysa her event için reflection ile (`MakeGenericType` + `Activator.CreateInstance`) `DomainEventNotification<T>` üretip `_mediator.Publish(object, ct)` ile yayınla → `ClearDomainEvents()`.

### Faz 3 — Repository Implementasyonları ✅
- `Persistence/Repositories/BookRepository.cs`, `LoanRepository.cs`, `MemberRepository.cs`, `UnitOfWork.cs` — hepsi yazıldı, build ve testler temiz.
- `Application` katmanında daha önce repository metoduna sahip olup hiçbir handler tarafından kullanılmayan boşluklar dolduruldu:
  - `Commands/DeleteBook` (soft-delete)
  - `Commands/DeleteMember` (soft-delete)
  - `Queries/GetLoanHistoryByMember` (status filtresiz, `GetActiveLoansByMemberQuery`'nin tam geçmiş versiyonu — aynı `LoanDto`'yu reuse ediyor)

### Faz 4 — Kimlik Doğrulama (ASP.NET Identity, JWT) ✅ (kod tarafı tamamlandı, appsettings hariç)

**4.1 — Tasarım kararı ✅**
- `ApplicationUser` (Infrastructure/Identity, giriş kimliği) ile `Member` (Domain, kütüphane üyeliği) **bilinçli olarak ayrı** tutuldu — Dependency Rule gereği Domain'in Identity'den haberi olmamalı.
- `ApplicationUser` → `Member` bağlantısı **tek yönlü, navigation'sız FK**: `ApplicationUser.MemberId` (nullable `Guid?`), Domain tarafında karşılık gelen bir alan yok (Faz 3'teki `HasOne<T>().WithMany().HasForeignKey` pattern'iyle tutarlı).
- `MemberId` sadece Customer rolündeki kullanıcılarda dolu; Admin/Employee'de `null` kalır. (Employee'ye özel domain verisi şu an yok, YAGNI gereği `Employee` entity'si açılmadı — ileride gerekirse aynı pattern: `ApplicationUser.EmployeeId` + ayrı `Employee` domain entity'si.)
- **Name/Surname/Email çakışma kararı:** Hem `Member` hem `ApplicationUser` üzerinde `Name`, `Surname`, `Email` alanları var (kullanıcı ayrı ayrı tutmayı tercih etti, `FullName` yerine). Register anında ikisine de **aynı değerler** yazılacak — ayrı senkronizasyon mekanizması yok, şu an "email/isim güncelleme" senaryosu scope dışı olduğu için tutarsızlık riski yok.
- Roller: `Admin`, `Employee`, `Customer` — Identity `AspNetRoles` üzerinden yönetiliyor, ayrı bir "rol" domain kavramı yok.

**4.2 — Identity Kurulumu ✅**
- `Infrastructure/Identity/ApplicationUser.cs`: `IdentityUser<Guid>`'den türüyor, ek alanlar: `Name`, `Surname`, `MemberId` (nullable).
- `Infrastructure/Identity/ApplicationRole.cs`: `IdentityRole<Guid>`'den türüyor, şimdilik boş (ileride role-özel alan gerekirse genişletilir).
- `Persistence/Configurations/ApplicationUserConfiguration.cs`: `Name`/`Surname` için `IsRequired` + `HasMaxLength(100)`; `MemberId` FK'i `HasOne<Member>().WithMany().HasForeignKey(u => u.MemberId).IsRequired(false).OnDelete(DeleteBehavior.SetNull)`.
- `Infrastructure/Identity/IdentitySeeder.cs`: `SeedRolesAsync(RoleManager<ApplicationRole>)` — `Admin`/`Employee`/`Customer` rollerini `RoleExistsAsync` kontrolüyle idempotent şekilde oluşturuyor. **Henüz hiçbir yerden çağrılmadı** (API projesi olmadığı için `Program.cs` yok) — Faz 7.1'de API kurulunca `app.Services.CreateScope()` içinde çağrılacak.
- `LibraryDbContext`, `IdentityDbContext<ApplicationUser, ApplicationRole, Guid>`'e dönüştürüldü (kullanıcı kendi yaptı).

**4.3 — JWT ✅ (appsettings hariç)**
- `Application/Common/IJwtTokenGenerator.cs`: `GenerateToken(Guid userId, string userName, IList<string> roles)` — **bilinçli olarak `ApplicationUser` parametre almıyor**, primitive tipler alıyor (Application'ın Infrastructure'a bağımlı olmaması için, Faz 0'daki MediatR ayrımıyla aynı mantık).
- `Infrastructure/Authentication/JwtTokenGenerator.cs`: `IConfiguration`'dan `Jwt:SecretKey`/`Jwt:Issuer`/`Jwt:Audience`/`Jwt:ExpiryMinutes` okuyor, `ClaimTypes.NameIdentifier`/`ClaimTypes.Name`/`ClaimTypes.Role` (her rol için ayrı claim) claim'leriyle token üretiyor. SecretKey eksikse `InvalidOperationException` fırlatıyor (sessiz null yerine erken/net hata).
- **appsettings.json henüz yok** — bu, `LibraryApp.API` projesi olmadığı için normal, Faz 7.1'de eklenecek (`Jwt:Issuer`, `Jwt:Audience`, `Jwt:SecretKey`, `Jwt:ExpiryMinutes`, `ConnectionStrings:DefaultConnection`).

**4.4 — Cookie Auth ⏸️ ertelendi**
- Bu tamamen `LibraryApp.WebUI`'ye ait (Infrastructure'da yapılacak bir şey yok) — Faz 7.2'de yazılacak.

**4.5 — CurrentUserService ✅**
- `Infrastructure/Services/CurrentUserService.cs`: `ICurrentUserService`'i implemente ediyor, `IHttpContextAccessor` ile `HttpContext.User`'dan `ClaimTypes.NameIdentifier` → `UserId` (Guid.TryParse ile güvenli parse, bulunamazsa `Guid.Empty`), `ClaimTypes.Name` → `UserName`, `User.IsInRole("Admin")` → `IsAdmin` okuyor. Claim isimleri `JwtTokenGenerator`'daki isimlerle birebir eşleşiyor.

**4.6 — Rol kararları ⏸️ ertelendi**
- Hangi Command'ı hangi rolün yapabileceği (örn. `AddBookCommand` → Admin/Employee, `BorrowBookCommand` → Customer da) **bilinçli olarak Faz 7'ye bırakıldı** — controller'lar yazılırken `[Authorize(Roles = "...")]` ile birlikte somut olarak kararlaştırılacak, şimdiden soyut liste çıkarmak gerçek bağlamdan kopuk olurdu.

**DI Kaydı (Faz 5'in parçası, Faz 4 boyunca kademeli yapıldı) ✅**
- `InfrastructureServiceRegistration.cs`: `AddDbContext<LibraryDbContext>` (Npgsql), repository kayıtları, `AddIdentity<ApplicationUser, ApplicationRole>().AddEntityFrameworkStores<LibraryDbContext>().AddDefaultTokenProviders()`, `IJwtTokenGenerator`, `ICurrentUserService`, `AddHttpContextAccessor()`. Build temiz.

**Not — Task dosyasında `RegisterCommand`/`LoginCommand` hiçbir fazda satır satır yazılı değil**, sadece varlığından bahsediliyor (Faz 4.1 ve Ilerleme'deki "karar 9"). Bunlar henüz yazılmadı, muhtemelen Faz 7'de (Controller'larla birlikte, `AuthController`/`AccountController`) ele alınacak — ama migration'dan önce de yazılabilir, bu konuşulmadı, karar verilmedi.

---

## Önemli Mimari Kararlar

1. **Soft delete**: `Entity.cs`'e public `MarkAsDeleted()` eklendi (`IsDeleted = true; UpdatedAt = DateTime.UtcNow;`). `IsDeleted` setter'ı `protected` olduğu için repository'ler bunu doğrudan set edemiyordu, bu yüzden Domain'e bu metot eklendi. Hard delete yok, hiçbir yerde.
2. **Global Query Filter**: 4 configuration'da da `builder.HasQueryFilter(e => !e.IsDeleted);` var — soft-deleted kayıtlar otomatik olarak her sorgudan hariç tutuluyor, repository kodunda elle filtre yazmaya gerek yok.
3. **Value Object mapping (`OwnsOne`)**: `ISBN`, `Money`, `LoanPeriod` owned type olarak map edildi. `LoanPeriod`'un constructor parametre isimleri (`start`, `due`) property isimleriyle (`BorrowedAt`, `ExpectedReturnDate`) eşleşmediği için EF'in constructor binding'i çalışmıyordu — parametre isimleri `borrowedAt`/`expectedReturnDate` olarak değiştirildi (Domain'de kozmetik bir rename, dışarıya etkisi yok).
4. **FK ilişkileri navigation'sız**: `Book.AuthorId`, `Loan.BookId`, `Loan.MemberId`, ve yeni olarak `ApplicationUser.MemberId` için karşı tarafta navigation property yok (bilinçli sadelik). Bu yüzden `HasOne<T>().WithMany().HasForeignKey(x => x.XId)` kalıbı tutarlı şekilde kullanıldı.
5. **`DomainEvents` ignore**: `BookConfiguration` ve `LoanConfiguration`'da `builder.Ignore(x => x.DomainEvents);` var (AggregateRoot'un bu property'si persist edilmemeli).
6. **Unit of Work deseni**: Repository metotları (`AddAsync`, `UpdateAsync`, `DeleteAsync`) **kendi içlerinde `SaveChangesAsync` çağırmıyor**. Kaydetme her zaman handler'ın sonunda tek bir `_unitOfWork.SaveChangesAsync()` çağrısıyla oluyor.
7. **`UpdateAsync` implementasyonu**: `_context.Set.Update(entity)` + `Task.CompletedTask` — entity zaten tracked olsa da zararsız, değilse attach edip `Modified` işaretliyor.
8. **`IAuthorRepository` yok, bilinçli**: Author'ı doğrudan kullanan bir Command/Query olmadığı için Domain'de interface'i yok, Infrastructure'da da implementasyonu yok.
9. **Member "ekleme" command'ı bilerek yazılmadı**: `RegisterCommand`, hem `ApplicationUser` hem `Member`'ı birlikte oluşturacak — henüz yazılmadı, Faz 4 sadece altyapıyı (Identity, JWT, CurrentUserService) hazırladı.
10. **`ApplicationUser`/`Member` ayrımı (Faz 4.1)**: Identity = "giriş kimliği" (Infrastructure), Member = "kütüphane üyeliği" (Domain). `PasswordHash`, `UserName`, roller sadece `ApplicationUser`'da; `Name`/`Surname`/`Email` her ikisinde de var (senkron, tek yönlü kopyalama, Register anında).
11. **`IJwtTokenGenerator` primitive parametre alır**: `ApplicationUser` tipini Application katmanına sızdırmamak için `GenerateToken(Guid userId, string userName, IList<string> roles)` imzası kullanıldı.
12. **Claim isimleri tutarlılığı**: `JwtTokenGenerator`'ın ürettiği claim'ler (`ClaimTypes.NameIdentifier`, `ClaimTypes.Name`, `ClaimTypes.Role`) ile `CurrentUserService`'in okuduğu claim'ler birebir aynı — biri değişirse diğeri de değişmeli.

---

## Çalışma Yöntemimiz

- Kullanıcı kodu genelde kendi yazıyor, küçük parçalar halinde ilerliyor ve "bak/kontrol et" diyor — ben gerçek dosyayı okuyup `dotnet build` (bazen `dotnet test`) ile doğruluyorum, sadece "doğru görünüyor" demiyorum.
- Kullanıcı açıkça "sen yaz" dediğinde ben doğrudan implement ediyorum.
- Yeni bir konsepte geçmeden önce önce mantığı/nedenini anlatıyorum, kod yazmadan; kullanıcı onayladıktan/denedikten sonra devam ediyoruz.
- Task dosyasındaki plana sadık kalınıyor ama kör kör uygulanmıyor — Faz 4.6 gibi bazı adımlar bilinçli olarak daha sonraki, daha somut bir faza ertelendi.

---

## Git Durumu

- Son commit: `3703542 Add Infrastructure layer: EF Core persistence and domain event dispatch` (DbContext + configuration iskeleti + event dispatch, soft-delete öncesi hâli).
- **Commitlenmemiş değişiklikler birikti**: `Entity.cs` (MarkAsDeleted), 4 configuration dosyası (HasQueryFilter), `Persistence/Repositories/` (Book/Loan/Member/UnitOfWork), `Commands/DeleteBook`, `Commands/DeleteMember`, `Queries/GetLoanHistoryByMember`, ve şimdi Faz 4'ün tamamı: `Identity/ApplicationUser.cs`, `Identity/ApplicationRole.cs`, `Identity/IdentitySeeder.cs`, `Persistence/Configurations/ApplicationUserConfiguration.cs`, `Authentication/JwtTokenGenerator.cs`, `Services/CurrentUserService.cs`, `Application/Common/IJwtTokenGenerator.cs`, güncellenmiş `LibraryDbContext.cs` ve `InfrastructureServiceRegistration.cs`.
- Henüz commit atılmadı — muhtemelen Faz 4 bittiğinde mantıklı bir commit noktası.

---

## Sırada Ne Var (yarın buradan devam)

**Açık karar — önce şu ikisinden birine karar verilecek:**
1. **Faz 6 (Migration)**'a direkt geç — Task dosyasının sırası bu. Value object mapping'leri (ISBN/Money/LoanPeriod `OwnsOne`) ve yeni Identity tablolarını (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`) doğrular. `LibraryApp.API` henüz olmadığı için migration komutunu geçici bir startup projesiyle ya da doğrudan Infrastructure hedefleyerek atmak gerekecek.
2. **`RegisterCommand`'ı önce yaz** — Task dosyasında hiçbir fazda satır satır yazılı değil (sadece bahsi geçiyor), ama migration sonrası akışı uçtan uca test edebilmek için mantıklı olabilir.

Kullanıcı henüz bu ikisi arasında seçim yapmadı — yarın buradan devam edilecek.

**Sonrasında:**
- Faz 6 — Migration (`dotnet ef migrations add InitialCreate`), `dotnet ef database update`, Identity tablolarının doğrulanması.
- Faz 7.1 — `LibraryApp.API` projesi: `appsettings.json` (Jwt ayarları + connection string burada eklenecek), JWT middleware, `AuthController`/`BooksController`/`LoansController`/`MembersController`, `IdentitySeeder.SeedRolesAsync` çağrısı `Program.cs`'te.
- Faz 4.6 — Rol/Command yetkilendirme kararları (Faz 7.1 ile birlikte, controller'lar yazılırken).
- Faz 7.2 — `LibraryApp.WebUI` (Cookie auth, Faz 4.4 burada devreye girecek).
