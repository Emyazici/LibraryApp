# LibraryApp — Infrastructure Katmanı Görev Listesi

> Repo incelendi: `LibraryApp.Domain` ve `LibraryApp.Application` mevcut, `LibraryApp.Infrastructure`, `LibraryApp.API` ve `LibraryApp.WebUI` henüz yok. Aşağıdaki liste, mevcut interface'lere (IBookRepository, ILoanRepository, IMemberRepository, IUnitOfWork, ICurrentUserService) birebir uyacak şekilde hazırlandı.
>
> **Mimari karar (bu revizyonda eklendi):** Presentation katmanında **iki ayrı proje** olacak — `LibraryApp.API` (mobil için, JWT) ve `LibraryApp.WebUI` (tarayıcı için, MVC + Cookie). İkisi de birbirine HTTP ile istek atmıyor; ikisi de `LibraryApp.Application`'a **doğrudan proje referansı** veriyor ve aynı `IMediator`/`ISender` üzerinden Command/Query gönderiyor. Aralarında network hop yok. Ortak nokta: aynı `LibraryDbContext`, aynı Identity tabloları (`AspNetUsers`, `AspNetRoles`), aynı veritabanı. Farklı nokta: kimlik taşıma şekli — API'de JWT (Bearer token), WebUI'de Cookie.

---

## FAZ 0 — Ön Temizlik (Infrastructure'a geçmeden önce)

### 0.1 — Domain'in MediatR bağımlılığını kır
Şu an `LibraryApp.Domain.csproj` içinde `MediatR` paket referansı var ve `IDomainEvent`, MediatR'ın `INotification`'ından türüyor. Bu, Clean Architecture'ın Dependency Rule'ını ihlal ediyor.

- [ ] `LibraryApp.Domain/Common/IDomainEvent.cs` dosyasından `INotification` referansını kaldır:
  ```csharp
  public interface IDomainEvent
  {
      Guid Id { get; }
      DateTime OccurredOn { get; }
  }
  ```
- [ ] `LibraryApp.Domain.csproj`'dan `<PackageReference Include="MediatR" ... />` satırını sil.
- [ ] `LibraryApp.Application` katmanında bir adaptör oluştur (MediatR'ın `INotification`'ını Domain event'ine sarmalayan):
  ```csharp
  // Application/Events/DomainEventNotification.cs
  public class DomainEventNotification<TDomainEvent> : INotification
      where TDomainEvent : IDomainEvent
  {
      public TDomainEvent DomainEvent { get; }
      public DomainEventNotification(TDomainEvent domainEvent) => DomainEvent = domainEvent;
  }
  ```
- [ ] Testleri çalıştırıp (`LibraryApp.Tests`) hiçbir şeyin kırılmadığını doğrula (testler zaten sadece `DomainEvents` listesini kontrol ediyor, MediatR'a dokunmuyor — sorun çıkmamalı).

---

## FAZ 1 — Proje İskeleti

- [ ] `LibraryApp.Infrastructure` adında yeni bir Class Library projesi oluştur.
- [ ] Proje referansları ekle:
  - `LibraryApp.Infrastructure` → `LibraryApp.Domain` (repository interface'lerini implemente etmek için)
  - `LibraryApp.Infrastructure` → `LibraryApp.Application` (event dispatch için `IMediator` ve `DomainEventNotification<T>` kullanmak için)
- [ ] Gerekli NuGet paketlerini ekle:
  - `Microsoft.EntityFrameworkCore`
  - `Microsoft.EntityFrameworkCore.SqlServer` (ya da tercih ettiğin DB provider'ı)
  - `Microsoft.EntityFrameworkCore.Design`
  - `Microsoft.Extensions.Configuration.Abstractions`
  - Identity için: `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
  - JWT için: `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.IdentityModel.Tokens`, `System.IdentityModel.Tokens.Jwt`

---

## FAZ 2 — Persistence (EF Core)

### 2.1 — DbContext
- [ ] `Persistence/LibraryDbContext.cs` oluştur. `DbSet<Book>`, `DbSet<Loan>`, `DbSet<Member>`, `DbSet<Author>` ekle.
- [ ] `OnModelCreating` içinde `ApplyConfigurationsFromAssembly` çağır (aşağıdaki config sınıflarını otomatik yükler).

### 2.2 — Entity Configuration sınıfları
`Persistence/Configurations/` klasörü altında her entity için `IEntityTypeConfiguration<T>` yaz:

- [ ] `BookConfiguration.cs` — **dikkat:** `ISBN` ve `Money` value object'leri `sealed class`, private constructor'lı ve get-only property'lere sahip. Bunları `OwnsOne()` ile configure ederken EF Core'un private constructor'ı kullanabilmesi için ya backing field yaklaşımı ya da `UsePropertyAccessMode(PropertyAccessMode.Field)` gerekebilir — bunu test ederken ilk migration'da mutlaka doğrula.
- [ ] `LoanConfiguration.cs` — `LoanPeriod` value object'i için aynı `OwnsOne()` yaklaşımı.
- [ ] `MemberConfiguration.cs`
- [ ] `AuthorConfiguration.cs` (repo'da entity var ama henüz repository interface'i yok — Book ile ilişkisini kur, en azından FK olarak)

### 2.3 — Domain Event Dispatch (asıl konuştuğumuz eksik parça)
- [ ] `LibraryDbContext` içinde `SaveChangesAsync`'i override et:
  ```csharp
  public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
  {
      // 1. Save öncesi, event'i olan tüm AggregateRoot'ları topla
      var aggregatesWithEvents = ChangeTracker.Entries<AggregateRoot>()
          .Select(e => e.Entity)
          .Where(e => e.DomainEvents.Any())
          .ToList();

      // 2. Önce veritabanına yaz
      var result = await base.SaveChangesAsync(ct);

      // 3. Save başarılıysa event'leri yayınla
      foreach (var aggregate in aggregatesWithEvents)
      {
          var events = aggregate.DomainEvents.ToList();
          aggregate.ClearDomainEvents();

          foreach (var domainEvent in events)
              await _mediator.Publish(WrapAsNotification(domainEvent), ct);
      }

      return result;
  }
  ```
- [ ] `IMediator`'ı constructor injection ile al (`LibraryApp.Application` referansı sayesinde erişilebilir).
- [ ] `DomainEventNotification<T>` wrap işlemini reflection ile genel bir metoda çevir (event tipi runtime'da bilinmediği için `dynamic` ya da reflection gerekecek — bu kısımda takılırsan birlikte kod yazalım).

---

## FAZ 3 — Repository Implementasyonları

`Persistence/Repositories/` klasörü altında, Domain'deki interface'lere **birebir** uyacak şekilde:

- [ ] `BookRepository : IBookRepository`
  - `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`, `ExistsByIsbnAsync`, `GetByIdsAsync`
- [ ] `LoanRepository : ILoanRepository`
  - `GetByIdAsync`, `GetByMemberIdAsync`, `GetActiveLoansByMemberAsync`, `HasActiveLoanAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`
- [ ] `MemberRepository : IMemberRepository`
  - `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`, `ExistsByIdAsync`
- [ ] `UnitOfWork : IUnitOfWork`
  - Tek metodu var: `SaveChangesAsync` → doğrudan `_context.SaveChangesAsync(ct)` çağırır (event dispatch zaten DbContext override'ında oluyor).

> Not: `DeleteAsync` metodları — Entity'de `IsDeleted` alanı zaten var, muhtemelen soft-delete istiyorsun. Hard delete mi yoksa `IsDeleted = true` set edip update mi yapacaksın, buna karar ver ve tüm repository'lerde tutarlı uygula.

---

## FAZ 4 — Kimlik Doğrulama (ASP.NET Identity, JWT + Cookie)

### 4.1 — Tasarım kararı (önce bunu netleştirelim)
`Member` şu an salt bir Domain entity (Name, Email) — kimlikle ilgili hiçbir alanı yok. ASP.NET Identity'nin kendi `IdentityUser`'ı zaten Username/PasswordHash/Roles gibi alanları getiriyor, bu yüzden LDAP'taki gibi "shadow account" ihtiyacı yok, ama yine de bir eşleme kararı gerekiyor:

- [ ] **Karar ver:** `ApplicationUser : IdentityUser<Guid>` sınıfı oluşturup, buna bir `MemberId` (nullable FK) ekleyerek Domain'deki `Member` ile ilişkilendireceğiz. Yani:
  - `ApplicationUser` = "sisteme giriş kimliği" (Identity'nin yönettiği tablo — Infrastructure'da yaşar)
  - `Member` = "kütüphane üyeliği" (Domain kavramı, iş kuralları burada)
  - Bir kullanıcı kayıt olunca hem `ApplicationUser` hem karşılık gelen `Member` oluşturulur.
  - Bu ayrım DDD açısından doğru: Identity, framework'e özgü bir altyapı meselesi; Domain'in bundan haberi olmamalı.
  - **Not (bu revizyonda eklendi):** Bu `ApplicationUser`/`Member` ayrımı hem API hem WebUI için ortak — ikisi de aynı tabloyu okuyup yazacak. Kayıt (Register) akışı hangi projeden tetiklenirse tetiklensin (mobil → API, tarayıcı → WebUI), aynı `Application/Commands/Auth/RegisterCommand` çalışır.

### 4.2 — ASP.NET Identity Kurulumu
- [ ] `ApplicationUser.cs` oluştur: `public class ApplicationUser : IdentityUser<Guid> { public Guid? MemberId { get; set; } }`
- [ ] `LibraryDbContext`'i `IdentityDbContext<ApplicationUser, ApplicationRole, Guid>`'den türet (rol bazlı yönetim için `ApplicationRole : IdentityRole<Guid>` de tanımla).
- [ ] `builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()` kaydını `InfrastructureServiceRegistration`'a ekle — `AddEntityFrameworkStores<LibraryDbContext>()` ile zincirle.
- [ ] Roller: `Customer`, `Employee`, `Admin` — uygulama ilk açıldığında (seed) bu üç rolü otomatik oluşturan bir `IdentitySeeder` yaz (yoksa DB'de rol bulunmaz, login'de hata alırsın).
  - **Not:** Seeder'ı iki projeden birinde (örn. API'nin `Program.cs`'inde) bir kere çalıştırmak yeterli, çünkü ikisi de aynı veritabanını paylaşıyor. WebUI'de tekrar çalıştırırsan idempotent olduğundan (`RoleExistsAsync` kontrolü) sorun çıkmaz, ama gereksiz.

### 4.3 — JWT (sadece API için)
- [ ] `Application/Common/IJwtTokenGenerator.cs` interface'i tanımla — girdi: `ApplicationUser` + roller, çıktı: token string.
- [ ] `Infrastructure/Authentication/JwtTokenGenerator.cs` — token üretimi. Claims'e en az şunları koy: `ClaimTypes.NameIdentifier` (UserId), `ClaimTypes.Name` (UserName), `ClaimTypes.Role` (her rol için bir claim — birden fazla rolü olabilir).
- [ ] `appsettings.json`'a JWT ayarlarını ekle: `Issuer`, `Audience`, `SecretKey`, `ExpiryMinutes`. (Bu ayarlar sadece `LibraryApp.API` projesinde okunacak — WebUI'nin JWT ayarlarına ihtiyacı yok.)

### 4.4 — Cookie Authentication (sadece WebUI için — bu revizyonda eklendi)
- [ ] `LibraryApp.WebUI/Program.cs` içinde `AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options => {...})` ekle.
- [ ] Cookie ayarları:
  ```csharp
  options.Cookie.HttpOnly = true;
  options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
  options.Cookie.SameSite = SameSiteMode.Strict;
  options.LoginPath = "/Account/Login";
  options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
  options.SlidingExpiration = true;
  ```
- [ ] JWT ile ortak nokta: `SignInManager<ApplicationUser>.PasswordSignInAsync()` de aynı `UserManager.CheckPasswordAsync()` doğrulamasını kullanır — API'deki `LoginCommand` handler'ıyla **doğrulama mantığı ortak**, sadece son adımda JWT üretmek yerine `SignInAsync()` çağrılıp cookie basılır.
- [ ] Bu yüzden `Application/Commands/Auth/LoginCommand`'ı **iki farklı sonuç** döndürecek şekilde tasarlama — handler sadece "doğrulama başarılı mı, kullanıcı/roller ne" bilgisini döndürsün. Token üretimi (API) ya da cookie basma (WebUI), Presentation katmanının kendi işi olsun (handler'ı ikisi için de aynı bırak, sonrasını controller'a bırak).

### 4.5 — ICurrentUserService implementasyonu
- [ ] `ICurrentUserService`, `HttpContext.User` (ClaimsPrincipal) okuyacağı için `IHttpContextAccessor`'a ihtiyaç duyar. `Microsoft.AspNetCore.Http.Abstractions` paketini Infrastructure'a ekle.
- [ ] `CurrentUserService : ICurrentUserService` — `UserId`'yi `ClaimTypes.NameIdentifier`'dan, `UserName`'i `ClaimTypes.Name`'den, `IsAdmin`'i `User.IsInRole("Admin")` ile çek.
- [ ] **Not:** Bu implementasyon hem API hem WebUI için aynı çalışır — `ClaimsPrincipal` her iki authentication scheme'de de (JWT decode edilince ya da cookie decrypt edilince) aynı şekilde `HttpContext.User`'a doluyor. `ICurrentUserService`'in kendisi hangi scheme'in aktif olduğunu bilmez, bilmesine de gerek yok.

### 4.6 — Authorization (Faz 7'de API ve WebUI'de kullanılacak ama tanımı burada netleşiyor)
- [ ] Controller/endpoint bazında hangi rolün hangi işlemi yapabileceğine karar ver, örnek:
  - `AddBookCommand` → sadece `Admin`/`Employee`
  - `BorrowBookCommand`, `ReturnBookCommand` → `Customer` da yapabilir
  - Bu kararları not al, Faz 7'de **hem API'de `[Authorize(Roles = "...")]` hem WebUI'de aynı attribute** olarak uygulanacak — rol kuralları iki projede de birebir aynı, çünkü aynı roller aynı veritabanında tutuluyor.

---

## FAZ 5 — DI Kaydı

- [ ] `InfrastructureServiceRegistration.cs` oluştur (Application'daki `ApplicationServiceRegistration` pattern'iyle tutarlı). **Bu metot hem API hem WebUI'nin `Program.cs`'inde çağrılacak** — repository'ler, DbContext, Identity kaydı ortak:
  ```csharp
  public static IServiceCollection AddInfrastructureServices(
      this IServiceCollection services, IConfiguration configuration)
  {
      services.AddDbContext<LibraryDbContext>(options =>
          options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

      services.AddScoped<IBookRepository, BookRepository>();
      services.AddScoped<ILoanRepository, LoanRepository>();
      services.AddScoped<IMemberRepository, MemberRepository>();
      services.AddScoped<IUnitOfWork, UnitOfWork>();

      services.AddIdentity<ApplicationUser, ApplicationRole>()
          .AddEntityFrameworkStores<LibraryDbContext>()
          .AddDefaultTokenProviders();

      services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>(); // API kullanır, WebUI kullanmaz ama kayıtlı olması sorun değil
      services.AddScoped<ICurrentUserService, CurrentUserService>();
      services.AddHttpContextAccessor();

      return services;
  }
  ```
- [ ] **Not (bu revizyonda eklendi):** `AddAuthentication(...)` bilerek bu metodun **dışında** tutuldu — çünkü API JWT scheme'i, WebUI Cookie scheme'i kaydedecek. Bu ikisi projeye özel olduğu için her `Program.cs` kendi authentication scheme'ini kendi kaydeder (bkz. Faz 7.1 ve 7.2).

---

## FAZ 6 — Migration

- [ ] `dotnet ef migrations add InitialCreate --project LibraryApp.Infrastructure --startup-project LibraryApp.API` (API projesi henüz yoksa, geçici bir console/startup projesi ile de yapılabilir).
- [ ] `dotnet ef database update`
- [ ] Value object mapping'lerinde hata alırsan (private constructor sorunu) burada ortaya çıkar — ilk migration'da mutlaka test et.
- [ ] `IdentityDbContext`'e geçtiğin için migration'da `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles` gibi Identity tablolarının da oluştuğunu doğrula.
- [ ] İlk migration sonrası roller boş olacağı için, uygulama ilk açılışta (`Program.cs`'te veya bir seeder'da) `Customer`/`Employee`/`Admin` rollerini oluşturan kodu çalıştır — yoksa login akışında rol atarken hata alırsın.
- [ ] **Not:** Migration tek bir yerden (`LibraryApp.Infrastructure`) yönetiliyor, hem API hem WebUI aynı migration'ı, aynı veritabanını kullanacak — WebUI'nin kendine ait bir migration'ı olmayacak.

---

## FAZ 7 — Presentation Katmanları (API + WebUI, henüz repo'da yok)

Bu revizyonda Presentation iki ayrı projeye ayrılıyor. **İkisi de aynı Application/Infrastructure'a doğrudan proje referansı verir, aralarında HTTP yok.** Mobil (Flutter) sadece `LibraryApp.API`'ye, tarayıcı sadece `LibraryApp.WebUI`'ye istek atar.

### 7.1 — LibraryApp.API (mobil için, JWT)

- [ ] `LibraryApp.API` (ASP.NET Core Web API) projesi oluştur.
- [ ] Proje referansları: `LibraryApp.Application`, `LibraryApp.Infrastructure`.
- [ ] `Program.cs`: `AddApplicationServices()` + `AddInfrastructureServices(configuration)` çağır.
- [ ] JWT authentication middleware'i ekle: `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => {...})` — `TokenValidationParameters`'ı `appsettings.json`'daki Issuer/Audience/SecretKey ile doldur.
- [ ] `app.UseAuthentication()` ve `app.UseAuthorization()` middleware sırasını doğru koy (Authentication her zaman Authorization'dan önce).
- [ ] Role-based authorization policy'leri tanımla (Customer/Employee/Admin) — Faz 4.6'da belirlediğin kurallara göre.
- [ ] Controller'lar: `AuthController` (Register/Login endpoint'leri, `UserManager`/`SignInManager` + `IJwtTokenGenerator` kullanır, JSON döner), `BooksController`, `LoansController`, `MembersController` — bu üçü ince (thin) kalmalı, sadece `ISender` üzerinden mevcut Command/Query'leri çağırır (`AddBookCommand`, `BorrowBookCommand`, `ReturnBookCommand`, `GetBookByIdQuery`, `GetActiveLoansByMemberQuery`). İlgili endpoint'lere Faz 4.6'daki role göre `[Authorize(Roles = "...")]` ekle.
- [ ] `appsettings.json`: connection string, JWT ayarları (Issuer/Audience/SecretKey/ExpiryMinutes).

### 7.2 — LibraryApp.WebUI (tarayıcı için, MVC + Cookie — bu revizyonda eklendi)

- [ ] `LibraryApp.WebUI` (ASP.NET Core MVC) projesi oluştur.
- [ ] Proje referansları: `LibraryApp.Application`, `LibraryApp.Infrastructure` (API'dekiyle **birebir aynı** — WebUI, API'ye değil, doğrudan Core'a bağlanıyor).
- [ ] `Program.cs`: `AddApplicationServices()` + `AddInfrastructureServices(configuration)` çağır (API ile aynı çağrı).
- [ ] Cookie authentication middleware'i ekle: `AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options => {...})` — Faz 4.4'teki ayarlarla.
- [ ] `app.UseAuthentication()` ve `app.UseAuthorization()` middleware sırasını doğru koy.
- [ ] Role-based authorization aynı şekilde `[Authorize(Roles = "...")]` ile MVC controller/action'larına uygulanır — kurallar Faz 4.6'dakiyle birebir aynı.
- [ ] `AccountController` — `Login`/`Register`/`Logout` action'ları. `UserManager`/`SignInManager`'ı doğrudan enjekte eder (API'deki gibi ama JWT üretmek yerine `SignInManager.SignInAsync()` çağırır ve `RedirectToAction` ile yönlendirir).
- [ ] İş mantığı içeren controller'lar: `BookController`, `LoanController`, `MemberController` — bunlar da ince kalmalı, `ISender` üzerinden **aynı Command/Query'leri** (`AddBookCommand`, `BorrowBookCommand`, `GetBookByIdQuery` vb.) çağırır, farkı sadece `return View(result)` dönmesi (API'de `return Ok(result)`).
- [ ] `Views/` klasör yapısı: her controller için bir alt klasör (`Views/Book/Index.cshtml`, `Views/Book/Details.cshtml` vb.), ortak layout için `Views/Shared/_Layout.cshtml`.
- [ ] ViewModel'ler — Application'daki DTO'ları (Query sonuçları) doğrudan View'e göndermek yerine, formlardan gelen veriyi (`[Required]`, `[StringLength]` gibi client-side validasyon attribute'ları) taşıyan ayrı `ViewModels/` klasörü oluştur (Command'lara mapleme burada yapılır).
- [ ] Anti-forgery: form post yapan her action'a `[ValidateAntiForgeryToken]` ekle (Razor form tag helper'ları `@Html.AntiForgeryToken()`'ı otomatik ekler, sen sadece action'a attribute'u koyuyorsun).
- [ ] `appsettings.json`: sadece connection string (JWT ayarlarına ihtiyaç yok, cookie ayarları kod içinde).

### 7.3 — Ortak notlar (API ve WebUI arasında)

- [ ] İki proje de aynı `Application/Commands` ve `Application/Queries`'i çağırdığı için, **iş mantığında tek satır kod tekrarı olmamalı.** Eğer bir controller'da (API ya da WebUI) mantık yazma isteği duyarsan, bu bir uyarı işaretidir — o mantık Application'a taşınmalı.
- [ ] İki proje bağımsız `Program.cs`'e sahip, bağımsız deploy edilir (örn. API bir App Service'te, WebUI başka bir App Service'te), ama **aynı veritabanına** bağlanır (aynı connection string, farklı `appsettings.json` dosyalarında tutulabilir).
- [ ] CORS: WebUI'nin API'ye hiç HTTP isteği atmadığını unutma — bu yüzden WebUI için CORS ayarı gerekmez. CORS sadece API'nin, eğer ileride bir SPA/farklı origin'den çağrılırsa gerekecek bir konu.

---

## Önerilen Sıra (özet)

1. Faz 0 (MediatR temizliği) — 15 dk, ama önemli
2. Faz 1-2 (DbContext + Configuration + Event Dispatch) — en kritik ve en çok zaman alacak kısım
3. Faz 3 (Repository'ler) — mekanik ama dikkat gerektirir
4. Faz 4.1-4.2 (Identity tasarım kararı + kurulum) — Faz 6'daki migration'ın Identity tablolarını doğru oluşturması için bundan önce netleşmeli
5. Faz 6 (ilk migration) — hem value object mapping'lerini hem Identity tablolarını doğrular
6. Faz 4.3-4.6 (JWT + Cookie + rol kararları) ve Faz 5 (DI)
7. Faz 7.1 (API) — mobil tarafı için ilk Presentation projesi
8. Faz 7.2 (WebUI/MVC) — web tarafı için ikinci Presentation projesi, API'den sonra yapılması önerilir çünkü Command/Query'lerin API üzerinden bir kere test edilmiş olması WebUI'deki hataları ayıklamayı kolaylaştırır
