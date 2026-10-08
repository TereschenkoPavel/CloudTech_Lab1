using System.Collections.Concurrent;

namespace eventManager.Repository
{
    public interface IRepository<T> where T : class, IEntity
    {
        void Add(T entity);
        IEnumerable<T> GetAll();
        T? GetById(Guid id);
        void Delete(Guid id);
    }

    public class InMemoryRepository<T> : IRepository<T> where T : class, IEntity
    {
        private readonly ConcurrentDictionary<Guid, T> _storage = new();
        public void Add(T entity) => _storage[entity.Id] = entity;
        public IEnumerable<T> GetAll() => _storage.Values;
        public T? GetById(Guid id) => _storage.TryGetValue(id, out var entity) ? entity : null;
        public void Delete(Guid id) => _storage.TryRemove(id, out _);
    }
}
