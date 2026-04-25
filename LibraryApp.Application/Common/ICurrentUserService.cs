namespace LibraryApp.Application.Common;
public interface ICurrentUserService
{
    Guid   UserId   { get; }  // JWT'den gelen kullanıcı id'si
    string UserName { get; }  // JWT'den gelen kullanıcı adı
    bool   IsAdmin  { get; }  // rol kontrolü
}