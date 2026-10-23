namespace DentalAlameda.Domain.Common;

/// <summary>Regla de negocio violada. La API la traduce a 409/422.</summary>
public class DomainException(string message) : Exception(message);

/// <summary>Recurso no encontrado. La API la traduce a 404.</summary>
public class NotFoundException(string message) : Exception(message);
