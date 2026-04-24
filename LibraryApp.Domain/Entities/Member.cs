using LibraryApp.Domain.Common;
using LibraryApp.Domain.Exceptions;

namespace LibraryApp.Domain.Entities;

public class Member : Entity
{
	public string Name { get; private set; } 
	public string Email { get; private set; }

	private Member() {}

	public static Member Create(string name, string email)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new BusinessRuleException("Üye adı boş olamaz.");

		if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
			throw new BusinessRuleException("Geçerli bir e-posta adresi giriniz.");

		return new Member
		{
			Id = Guid.NewGuid(),
			Name = name,
			Email = email
		};
	}
}