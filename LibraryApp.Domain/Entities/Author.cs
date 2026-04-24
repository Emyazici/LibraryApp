using LibraryApp.Domain.Common;
using LibraryApp.Domain.Exceptions;

namespace LibraryApp.Domain.Entities;
public class Author : Entity
{
	public string Name { get; private set; } = null!;

	private Author() {}

	public static Author Create(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new BusinessRuleException("Yazar adı boş olamaz.");

		return new Author
		{
			Id = Guid.NewGuid(),
			Name = name
		};
	}
}