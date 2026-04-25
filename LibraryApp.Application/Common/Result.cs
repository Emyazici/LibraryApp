namespace LibraryApp.Application.Common;

// Başarı/hata durumunu taşır — exception atmak yerine bunu kullanıyoruz
// Beklenen iş akışı hataları için (bulunamadı, yetki yok vs.)
// Beklenmedik hatalar (db koptu, disk dolu) için exception fırlatılır
public class Result
{
	public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }
    protected Result(bool isSuccess, string error)
    {
        // Tutarsız durum olamaz
        if (isSuccess && error != string.Empty)
            throw new InvalidOperationException("Başarılı result hata içeremez");
        if (!isSuccess && error == string.Empty)
            throw new InvalidOperationException("Başarısız result hata mesajı içermeli");

        IsSuccess = isSuccess;
        Error = error;
    }
    public static Result Success() => new(true, string.Empty);
    public static Result Failure(string error) => new(false, error);
    public static Result<T> Success<T>(T value) => new(value, true, string.Empty);
    public static Result<T> Failure<T>(string error) => new(default, false, error);
}

// Değer taşıyan versiyon
public class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T? value, bool isSuccess, string error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    // Başarısız result'ın value'suna erişilemez
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Başarısız result'ın değerine erişilemez");
}
