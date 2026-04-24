namespace LibraryApp.Domain.Enums;

public enum BookStatus
{
    Available = 1, // rafta, ödünç verilebilir
    Loaned    = 2, // tüm stok ödünçte
    Reserved  = 3  // bir üye için ayrılmış
}