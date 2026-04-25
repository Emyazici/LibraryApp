using LibraryApp.Application.Common;
using MediatR;

namespace LibraryApp.Application.Commands.ReturnBook;

public record ReturnBookCommand(
    Guid LoanId
) : IRequest<Result>; // değer dönmüyor — Result<T> değil Result, çünkü sadece işlemin başarılı olup olmadığını dönüyoruz. Eğer işlem sırasında bir hata oluşursa, Result nesnesi hata mesajını içerecektir.