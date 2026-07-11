using LibraryApp.Domain.Common;
using LibraryApp.Domain.Exceptions;
using LibraryApp.Domain.ValueObjects;

namespace LibraryApp.Domain.Entities;

public class Member : Entity
{
	public string Name { get; private set; } = null!;
    public string Surname { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public Money Balance { get; set; }

    private Member() {}

	public static Member Create(string name,string surname, string email)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new BusinessRuleException("Üye adı boş olamaz.");

		if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
			throw new BusinessRuleException("Geçerli bir e-posta adresi giriniz.");

		return new Member
		{
			Id = Guid.NewGuid(),
			Name = name,
			Surname = surname,
			Email = email,
            Balance = Money.Create(0, "TRY") //Baslangicta 0 TL
        };
	}
}