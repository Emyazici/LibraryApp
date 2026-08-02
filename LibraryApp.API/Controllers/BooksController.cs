using LibraryApp.Application.Commands.AddBook;
using LibraryApp.Application.Commands.DeleteBook;
using LibraryApp.Application.Queries.GetBookById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryApp.API.Controllers;

public record AddBookRequest(Guid AuthorId, string Title, string ISBN, decimal Price, string Currency, int TotalStock);

[Route("api/[controller]")]
[Authorize]
public class BooksController : ApiControllerBase
{
    private readonly ISender _sender;
    public BooksController(ISender sender) => _sender = sender;

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LibraryApp.Application.Queries.GetBookById.BookDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetBookByIdQuery(id), ct);
        return FromResult(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Employee")]
    public async Task<ActionResult<Guid>> Add(AddBookRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(
            new AddBookCommand(request.AuthorId, request.Title, request.ISBN, request.Price, request.Currency, request.TotalStock),
            ct);
        return FromResult(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new DeleteBookCommand(id), ct);
        return FromResult(result);
    }
}
