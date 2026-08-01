using FluentValidation;

namespace LibraryApp.Application.Commands.AddBook;

public class AddBookCommandValidator : AbstractValidator<AddBookCommand>
{
	public AddBookCommandValidator()
	{
		RuleFor(x => x.Title)
			.NotEmpty().WithMessage("Kitap başlığı boş olamaz.")
			.MaximumLength(200)
			.WithMessage("Kitap başlığı 200 karakteri geçemez.");

		RuleFor(x => x.AuthorId)
			.NotEmpty()
			.WithMessage("Yazar bilgisi boş olamaz.");

		RuleFor(x => x.ISBN)
			.NotEmpty()
			.WithMessage("ISBN boş olamaz.")
			.Must(isbn => isbn.Replace("-", "").Replace(" ", "") is { Length: 10 or 13 } cleaned
				&& cleaned.All(char.IsDigit))
			.WithMessage("ISBN 10 veya 13 haneli rakamlardan oluşmalıdır (tire/boşluk kullanılabilir).");

		RuleFor(x => x.Price)
			.GreaterThan(0)
			.WithMessage("Fiyat sıfırdan büyük olmalıdır.");

		RuleFor(x => x.Currency)
			.NotEmpty().WithMessage("Para birimi boş olamaz.")
			.Length(3).WithMessage("Para birimi 3 karakter olmalıdır. Örnek: USD, EUR, TRY");

		RuleFor(x => x.TotalStock)
			.GreaterThan(0)
			.WithMessage("Toplam stok negatif veya 0 olamaz.");
	}
}