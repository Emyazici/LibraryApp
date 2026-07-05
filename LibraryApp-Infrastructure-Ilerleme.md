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
- **`Persistence/LibraryDbContext.cs`**: düz `DbContext` (henüz `IdentityDbContext` değil — o dönüşüm Faz 4.2'de yapılacak). `DbSet<Book/Loan/Member/Author>` var. Constructor `DbContextOptions<LibraryDbContext>` + `IMediator` alıyor.
- **`Persistence/Configurations/`**: `BookConfiguration`, `LoanConfiguration`, `MemberConfiguration`, `AuthorConfiguration` — hepsi tamam.
- **`SaveChangesAsync` override edildi** (domain event dispatch): save öncesi event'i olan `AggregateRoot`'ları topla → `base.SaveChangesAsync` → başarılıysa her event için reflection ile (`MakeGenericType` + `Activator.CreateInstance`) `DomainEventNotification<T>` üretip `_mediator.Publish(object, ct)` ile yayınla → `ClearDomainEvents()`.

### Faz 3 — Repository Implementasyonları ✅
- `Persistence/Repositories/BookRepository.cs`, `LoanRepository.cs`, `MemberRepository.cs`, `UnitOfWork.cs` — hepsi yazıldı, build ve testler temiz.
- `Application` katmanında daha önce repository metoduna sahip olup hiçbir handler tarafından kullanılmayan boşluklar dolduruldu:
  - `Commands/DeleteBook` (soft-delete)
  - `Commands/DeleteMember` (soft-delete)
  - `Queries/GetLoanHistoryByMember` (status filtresiz, `GetActiveLoansByMemberQuery`'nin tam geçmiş versiyonu — aynı `LoanDto`'yu reuse ediyor)

---

## Önemli Mimari Kararlar

1. **Soft delete**: `Entity.cs`'e public `MarkAsDeleted()` eklendi (`IsDeleted = true; UpdatedAt = DateTime.UtcNow;`). `IsDeleted` setter'ı `protected` olduğu için repository'ler bunu doğrudan set edemiyordu, bu yüzden Domain'e bu metot eklendi. Hard delete yok, hiçbir yerde.
2. **Global Query Filter**: 4 configuration'da da `builder.HasQueryFilter(e => !e.IsDeleted);` var — soft-deleted kayıtlar otomatik olarak her sorgudan hariç tutuluyor, repository kodunda elle filtre yazmaya gerek yok.
3. **Value Object mapping (`OwnsOne`)**: `ISBN`, `Money`, `LoanPeriod` owned type olarak map edildi. `LoanPeriod`'un constructor parametre isimleri (`start`, `due`) property isimleriyle (`BorrowedAt`, `ExpectedReturnDate`) eşleşmediği için EF'in constructor binding'i çalışmıyordu — parametre isimleri `borrowedAt`/`expectedReturnDate` olarak değiştirildi (Domain'de kozmetik bir rename, dışarıya etkisi yok).
4. **FK ilişkileri navigation'sız**: `Book.AuthorId`, `Loan.BookId`, `Loan.MemberId` için karşı tarafta navigation property yok (bilinçli sadelik). Bu yüzden `HasOne<T>().WithMany().HasForeignKey(x => x.XId)` kalıbı kullanıldı.
5. **`DomainEvents` ignore**: `BookConfiguration` ve `LoanConfiguration`'da `builder.Ignore(x => x.DomainEvents);` var (AggregateRoot'un bu property'si persist edilmemeli).
6. **Unit of Work deseni**: Repository metotları (`AddAsync`, `UpdateAsync`, `DeleteAsync`) **kendi içlerinde `SaveChangesAsync` çağırmıyor**. Kaydetme her zaman handler'ın sonunda tek bir `_unitOfWork.SaveChangesAsync()` çağrısıyla oluyor — birden fazla repository'de yapılan değişiklik tek transaction'da commitleniyor.
7. **`UpdateAsync` implementasyonu**: `_context.Set.Update(entity)` + `Task.CompletedTask` — entity zaten tracked olsa da zararsız, değilse attach edip `Modified` işaretliyor.
8. **`IAuthorRepository` yok, bilinçli**: Author'ı doğrudan kullanan bir Command/Query olmadığı için Domain'de interface'i yok, Infrastructure'da da implementasyonu yok. Author sadece Book üzerinden FK olarak var.
9. **Member "ekleme" command'ı bilerek yazılmadı**: Faz 4.1'de Register akışı hem `ApplicationUser` hem `Member`'ı birlikte oluşturacak tek bir `RegisterCommand` olacak. Şimdi ayrı bir "sadece Member oluştur" command'ı yazmak, Identity gelince çakışır/tekrar iş çıkarır.

---

## Çalışma Yöntemimiz

- Kullanıcı kodu genelde kendi yazıyor, küçük parçalar halinde ilerliyor ve "bak/kontrol et" diyor — ben gerçek dosyayı okuyup `dotnet build` (bazen `dotnet test`) ile doğruluyorum, sadece "doğru görünüyor" demiyorum.
- Kullanıcı açıkça "sen yaz" dediğinde ben doğrudan implement ediyorum (örn. Member/Author configuration'ları, LoanRepository/MemberRepository/UnitOfWork, Delete command'ları).
- Yeni bir konsepte geçmeden önce (örn. Faz 2.3, Faz 3 soft-delete) önce mantığı/nedenini anlatıyorum, kod yazmadan; kullanıcı onayladıktan/denedikten sonra devam ediyoruz.
- Task dosyasındaki plana sadık kalınıyor ama kör kör uygulanmıyor — gerçek entity/repository kodunu okuyup task'taki notlarla (örn. `LoanPeriod` constructor sorunu, `DomainEvents` ignore ihtiyacı) çapraz kontrol ediyoruz.

---

## Git Durumu

- Son commit: `3703542 Add Infrastructure layer: EF Core persistence and domain event dispatch` (DbContext + configuration iskeleti + event dispatch, soft-delete öncesi hâli).
- **Şu an commitlenmemiş değişiklikler var**: `Entity.cs` (MarkAsDeleted), 4 configuration dosyası (HasQueryFilter), yeni `Persistence/Repositories/` klasörü (Book/Loan/Member/UnitOfWork), yeni `Commands/DeleteBook`, `Commands/DeleteMember`, `Queries/GetLoanHistoryByMember`.
- Bunları henüz commit'lemedik — kullanıcı isterse bir sonraki adımda commit atılabilir.

---

## Sırada Ne Var

**Faz 4 — Kimlik Doğrulama (ASP.NET Identity, JWT + Cookie)**, henüz başlanmadı. İlk karar noktası: `ApplicationUser : IdentityUser<Guid>` + `MemberId` (nullable FK) tasarımı, `LibraryDbContext`'in `IdentityDbContext<ApplicationUser, ApplicationRole, Guid>`'e dönüştürülmesi (bkz. Task dosyası Faz 4.1-4.2). Register akışının hem `ApplicationUser` hem `Member`'ı birlikte oluşturacağı unutulmamalı (yukarıdaki karar 9 ile bağlantılı).
