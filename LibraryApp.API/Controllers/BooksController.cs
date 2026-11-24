using LibraryApp.API.Extensions;
using LibraryApp.Application.Commands.AddBook;
using LibraryApp.Application.Commands.DeleteBook;
using LibraryApp.Application.Queries.GetBookById;
using LibraryApp.Application.Queries.GetBooks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryApp.API.Controllers;

public record AddBookRequest(Guid AuthorId, string Title, string ISBN, decimal Price, string Currency, int TotalStock);

[ApiController]
[Route("api/books")]
[Authorize]
public class BooksController(ISender sender) : ControllerBase
{
    /// <summary>Tüm kitapları listeler (silinmişler hariç).</summary>
    [HttpGet]
    [ProducesResponseType<List<BookDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BookDto>>> GetAll(CancellationToken ct)
    {
        var result = await sender.Send(new GetBooksQuery(), ct);
        return result.ToActionResult();
    }

    /// <summary>Kitap detayını getirir.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<BookDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BookDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetBookByIdQuery(id), ct);
        return result.ToActionResult();
    }

    /// <summary>Yeni kitap ekler. Oluşan kitabın id'sini döner.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Employee")]
    [ProducesResponseType<Guid>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> Add(AddBookRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new AddBookCommand(request.AuthorId, request.Title, request.ISBN, request.Price, request.Currency, request.TotalStock),
            ct);
        return result.ToActionResult();
    }

    /// <summary>Kitabı siler (soft delete).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteBookCommand(id), ct);
        return result.ToActionResult();
    }
}
