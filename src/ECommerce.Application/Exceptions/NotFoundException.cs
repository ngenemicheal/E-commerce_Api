namespace ECommerce.Application.Exceptions;

public class NotFoundException : AppException
{
    public Type EntityType { get; }
    public object? EntityId { get; }

    public NotFoundException(Type entityType, object? entityId)
        : base($"Entity '{entityType.Name}' with id '{entityId}' was not found.")
    {
        EntityType = entityType;
        EntityId = entityId;
    }

    public NotFoundException(string message) : base(message)
    {
        EntityType = typeof(object);
    }
}

public class NotFoundException<TEntity> : NotFoundException
{
    public NotFoundException(object? id) : base(typeof(TEntity), id)
    {
    }
}
