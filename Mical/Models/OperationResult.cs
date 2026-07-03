namespace Mical.Models;

/// <summary>
/// Resultado simple de una operación de negocio: éxito o fallo con mensaje.
/// Permite a los Services comunicar errores de dominio sin lanzar excepciones
/// ni depender de ModelState.
/// </summary>
public class OperationResult
{
    public bool Succeeded { get; private init; }
    public string? Error { get; private init; }

    public static OperationResult Success() => new() { Succeeded = true };
    public static OperationResult Fail(string error) => new() { Succeeded = false, Error = error };
}

/// <summary>Resultado de una operación que además devuelve un valor en caso de éxito.</summary>
public class OperationResult<T>
{
    public bool Succeeded { get; private init; }
    public string? Error { get; private init; }
    public T? Value { get; private init; }

    public static OperationResult<T> Success(T value) => new() { Succeeded = true, Value = value };
    public static OperationResult<T> Fail(string error) => new() { Succeeded = false, Error = error };
}
