namespace LibraryApp.Domain.Enums;
public enum LoanStatus
{
	Active = 1,   // ödünç verilmiş, iade edilmemiş
	Returned = 2, // iade edilmiş
	Overdue = 3   // süresi geçmiş, iade edilmemiş
}