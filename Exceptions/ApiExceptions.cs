using System;

namespace BGarden.API.Exceptions
{
    /// <summary>
    /// Исключение, возникающее при отсутствии запрошенного ресурса
    /// </summary>
    public class ResourceNotFoundException : Exception
    {
        public ResourceNotFoundException(string message) : base(message) { }
    }

    /// <summary>
    /// Исключение, возникающее при нарушении бизнес-правил
    /// </summary>
    public class BusinessRuleException : Exception
    {
        public BusinessRuleException(string message) : base(message) { }
    }
} 