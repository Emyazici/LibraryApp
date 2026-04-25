using LibraryApp.Domain.Repositories;

namespace LibraryApp.Application.Common;
public abstract class BaseHandler
{
    protected readonly IUnitOfWork         UnitOfWork;
    protected readonly ICurrentUserService  CurrentUser;

    protected BaseHandler(
        IUnitOfWork        unitOfWork,
        ICurrentUserService currentUser)
    {
        UnitOfWork  = unitOfWork;
        CurrentUser = currentUser;
    }
}