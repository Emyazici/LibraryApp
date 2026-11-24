using FluentAssertions;
using LibraryApp.Application.Queries.GetBooks;
using LibraryApp.Domain.Entities;
using LibraryApp.Domain.Repositories;
using Moq;

namespace LibraryApp.Tests.Application;

public class GetBooksQueryHandlerTests
{
    private readonly Mock<IBookRepository> _bookRepo = new();

    private GetBooksQueryHandler Sut() => new(_bookRepo.Object);

    [Fact]
    public async Task Handle_WhenNoBooks_ReturnsEmptyList()
    {
        _bookRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Book>());

        var result = await Sut().Handle(new GetBooksQuery(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithBooks_MapsEachBookToDto()
    {
        var book = Book.Create(Guid.NewGuid(), "Clean Code", "9780134685991", 29.99m, "TRY", 5);
        _bookRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Book> { book });

        var result = await Sut().Handle(new GetBooksQuery(), default);

        result.Value.Should().ContainSingle();
        var dto = result.Value[0];
        dto.Id.Should().Be(book.Id);
        dto.Title.Should().Be("Clean Code");
        dto.Isbn.Should().Be("9780134685991");
        dto.Stock.Should().Be(5);
        dto.Status.Should().Be("Available");
    }
}
