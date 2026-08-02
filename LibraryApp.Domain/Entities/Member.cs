using LibraryApp.Domain.Common;
using LibraryApp.Domain.Exceptions;
using LibraryApp.Domain.ValueObjects;

namespace LibraryApp.Domain.Entities;

public class Member : Entity
{
	public string Name { get; private set; } = null!;
    public string Surname { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public Money Balance { get; private set; }

    // Keycloak'taki kullanıcının "sub" claim'i (UUID string). Domain, Keycloak'a bağımlı olmasın diye nötr isimlendirildi.
    public string? ExternalIdentityId { get; private set; }

    private Member() {}

	public static Member Create(string name,string surname, string email, string? externalIdentityId = null)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new BusinessRuleException("Üye adı boş olamaz.");

		if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
			throw new BusinessRuleException("Geçerli bir e-posta adresi giriniz.");

		// Member.Id, Keycloak "sub" claim'iyle aynı tutulur: GetActiveLoansByMemberQuery gibi handler'lar
		// ICurrentUserService.UserId'yi (JWT'deki sub) doğrudan MemberId olarak kullanıyor — ayrı bir eşleme
		// tablosuna/sorgusuna gerek kalmasın diye externalIdentityId varsa Id olarak o kullanılır.
		var id = externalIdentityId is not null && Guid.TryParse(externalIdentityId, out var externalGuid)
			? externalGuid
			: Guid.NewGuid();

		return new Member
		{
			Id = id,
			Name = name,
			Surname = surname,
			Email = email,
            Balance = Money.Create(0, "TRY"), //Baslangicta 0 TL
            ExternalIdentityId = externalIdentityId
        };
	}

    public void SetExternalIdentityId(string externalIdentityId)
    {
        if (string.IsNullOrWhiteSpace(externalIdentityId))
            throw new BusinessRuleException("ExternalIdentityId boş olamaz.");

        ExternalIdentityId = externalIdentityId;
    }
}