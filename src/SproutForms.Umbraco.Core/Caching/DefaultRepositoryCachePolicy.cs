using Umbraco.Cms.Core.Cache;
using Umbraco.Extensions;

namespace SproutForms.Umbraco.Core.Caching;

public class DefaultRepositoryCachePolicy<TEntity, TId> : IRepositoryCachePolicy<TEntity, TId>
    where TEntity : class
{
    private static readonly TEntity[] _emptyEntities = []; // const

    protected readonly IAppPolicyCache _cache;
    protected readonly RepositoryPolicyOptions<TEntity, TId> _options;

    protected readonly string _entityTypeCacheKey;

    public DefaultRepositoryCachePolicy(IAppPolicyCache cache, RepositoryPolicyOptions<TEntity, TId> options)
    {
        _cache = cache;
        _options = options ?? throw new ArgumentNullException(nameof(options));

        _entityTypeCacheKey = options.CacheBaseKey.IfNullOrWhiteSpace($"sproutForms_{typeof(TEntity).Name}_");
    }

    /// <inheritdoc />
    public void Create(TEntity entity, Action<TEntity> persistNew)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        try
        {
            persistNew(entity);

            ClearCache(entity);

            // just to be safe, we cannot cache an item without an identity
            _cache.Insert(GetEntityCacheKey(entity), () => entity, TimeSpan.FromMinutes(5), true);
        }
        catch
        {
            // if an exception is thrown we need to remove the entry from cache,
            // this is ONLY a work around because of the way
            // that we cache entities: http://issues.umbraco.org/issue/U4-4259
            _cache.Clear(GetEntityCacheKey(entity));

            // if there's a GetAllCacheAllowZeroCount cache, ensure it is cleared
            ClearBaseCache();

            throw;
        }
    }

    /// <inheritdoc />
    public void Update(TEntity entity, Action<TEntity> persistUpdated)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        try
        {
            persistUpdated(entity);

            ClearCache(entity);

            _cache.Insert(GetEntityCacheKey(entity), () => entity, TimeSpan.FromMinutes(5), true);
        }
        catch
        {
            // if an exception is thrown we need to remove the entry from cache,
            // this is ONLY a work around because of the way
            // that we cache entities: http://issues.umbraco.org/issue/U4-4259
            _cache.Clear(GetEntityCacheKey(entity));

            // if there's a GetAllCacheAllowZeroCount cache, ensure it is cleared
            ClearBaseCache();

            throw;
        }
    }

    /// <inheritdoc />
    public void Delete(TEntity entity, Action<TEntity> persistDeleted)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        try
        {
            persistDeleted(entity);
        }
        finally
        {
            // whatever happens, clear the cache
            ClearCache(entity);
        }
    }

    public void Delete(TId id, Action<TId> persistDeleted)
    {
        try
        {
            persistDeleted(id);
        }
        finally
        {
            // whatever happens, clear the cache
            ClearCache(id);
        }
    }

    /// <inheritdoc />
    public TEntity? Get(TId? id, Func<TId?, TEntity?> performGet)
    {
        var cacheKey = GetEntityCacheKey(id);
        TEntity? fromCache = _cache.GetCacheItem<TEntity>(cacheKey);

        // if found in cache then return else fetch and cache
        if (fromCache != null)
        {
            return fromCache;
        }

        TEntity? entity = performGet(id);

        if (entity != null)
        {
            InsertEntity(cacheKey, entity);
        }

        return entity;
    }

    /// <inheritdoc />
    public TEntity? GetCached(TId id)
    {
        var cacheKey = GetEntityCacheKey(id);
        return _cache.GetCacheItem<TEntity>(cacheKey);
    }

    /// <inheritdoc />
    public bool Exists(TId id, Func<TId, bool> performExists, Func<TId[], IEnumerable<TEntity>?> performGetAll)
    {
        // if found in cache the return else check
        var cacheKey = GetEntityCacheKey(id);
        TEntity? fromCache = _cache.GetCacheItem<TEntity>(cacheKey);
        return fromCache != null || performExists(id);
    }

    /// <inheritdoc />
    public TEntity[] GetAll(TId[]? ids, Func<TId[]?, IEnumerable<TEntity>?> performGetAll)
    {
        if (ids?.Length > 0)
        {
            // try to get each entity from the cache
            // if we can find all of them, return
            TEntity[] entities = ids.Select(GetCached).WhereNotNull().ToArray();
            if (ids.Length.Equals(entities.Length))
            {
                return entities; // no need for null checks, we are not caching nulls
            }
        }
        else
        {
            // get everything we have
            TEntity?[] entities = _cache.GetCacheItemsByKeySearch<TEntity>(_entityTypeCacheKey)
                .ToArray(); // no need for null checks, we are not caching nulls

            if (entities.Length > 0)
            {
                // if some of them were in the cache...
                if (_options.GetAllCacheValidateCount)
                {
                    // need to validate the count, get the actual count and return if ok
                    if (_options.PerformCount is not null)
                    {
                        var totalCount = _options.PerformCount();
                        if (entities.Length == totalCount)
                        {
                            return entities.WhereNotNull().ToArray();
                        }
                    }
                }
                else
                {
                    // no need to validate, just return what we have and assume it's all there is
                    return entities.WhereNotNull().ToArray();
                }
            }
            else if (_options.GetAllCacheAllowZeroCount)
            {
                // if none of them were in the cache
                // and we allow zero count - check for the special (empty) entry
                TEntity[]? empty = _cache.GetCacheItem<TEntity[]>(_entityTypeCacheKey);
                if (empty != null)
                {
                    return empty;
                }
            }
        }

        // cache failed, get from repo and cache
        TEntity[]? repoEntities = performGetAll(ids)?
            .WhereNotNull() // exclude nulls!
            .ToArray();

        // note: if empty & allow zero count, will cache a special (empty) entry
        InsertEntities(ids, repoEntities);

        return repoEntities ?? Array.Empty<TEntity>();
    }

    /// <inheritdoc />
    public void ClearAll() => _cache.ClearByKey(_entityTypeCacheKey);

    public virtual void ClearCache(TId id)
    {
        _cache.Clear(GetEntityCacheKey(id));
        ClearBaseCache();
    }

    public void ClearCache(TEntity entity)
    {
        _cache.ClearByKey(GetEntityCacheKey(entity));
        ClearBaseCache();
    }

    /// <summary>
    /// Gets entities by a secondary property value, using a secondary index cache that stores lists of IDs.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property to filter by</typeparam>
    /// <param name="propertyValue">The value of the property to search for</param>
    /// <param name="performGetByProperty">Function to fetch entities from the repository when not cached</param>
    /// <param name="propertyName">The name of the property being indexed (used for cache key generation)</param>
    /// <returns>Array of entities matching the property value, or empty array if none found</returns>
    public TEntity[] GetByProperty<TProperty>(
        TProperty propertyValue,
        Func<TProperty, IEnumerable<TEntity>?> performGetByProperty,
        string propertyName)
    {
        if (propertyValue == null)
            return [];

        var secondaryIndexKey = GetPropertyCacheKey(propertyName, propertyValue);

        // Try to get the list of entity IDs from the secondary index
        var cachedIds = _cache.GetCacheItem<TId[]>(secondaryIndexKey);
        if (cachedIds != null)
        {
            // Resolve IDs back to entities from the main cache
            return cachedIds.Select(GetCached).WhereNotNull().ToArray();
        }

        // Secondary index miss - fetch from repository
        var entities = performGetByProperty(propertyValue)?
            .WhereNotNull()
            .ToArray();

        if (entities?.Length > 0)
        {
            var ids = entities.Select(_options.GetEntityId).ToArray();

            // Cache the secondary index (list of IDs)
            _cache.Insert(secondaryIndexKey, () => ids, TimeSpan.FromMinutes(5), true);

            // Also cache individual entities in the main cache
            foreach (var entity in entities)
            {
                InsertEntity(GetEntityCacheKey(entity), entity);
            }

            return entities;
        }
        else if (_options.GetAllCacheAllowZeroCount)
        {
            // Cache empty result if configured to allow zero-count caching
            _cache.Insert(secondaryIndexKey, () => (TId[])[], TimeSpan.FromMinutes(5), true);
        }

        return [];
    }

    /// <summary>
    /// Clears the secondary index cache for a specific property value.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property</typeparam>
    /// <param name="propertyName">The name of the property</param>
    /// <param name="propertyValue">The value of the property to clear</param>
    public void ClearPropertyCache<TProperty>(string propertyName, TProperty propertyValue)
    {
        var key = GetPropertyCacheKey(propertyName, propertyValue);
        _cache.Clear(key);
    }

    /// <summary>
    /// Clears all secondary indexes for a specific property name.
    /// </summary>
    /// <param name="propertyName">The name of the property to clear all indexes for</param>
    public void ClearPropertyCache(string propertyName)
    {
        _cache.ClearByKey($"{_entityTypeCacheKey}_{propertyName}_");
    }

    protected virtual void ClearBaseCache()
    {
        _cache.Clear(_entityTypeCacheKey);
    }

    protected string GetEntityCacheKey(TEntity entity) => _entityTypeCacheKey + _options.GetEntityId(entity);

    protected string GetEntityCacheKey(TId? id)
    {
        if (EqualityComparer<TId>.Default.Equals(id, default))
        {
            return string.Empty;
        }

        if (typeof(TId).IsValueType)
        {
            return _entityTypeCacheKey + id;
        }

        return _entityTypeCacheKey + id?.ToString()?.ToUpperInvariant();
    }

    /// <summary>
    /// Generates a cache key for a secondary property index.
    /// </summary>
    private string GetPropertyCacheKey<TProperty>(string propertyName, TProperty propertyValue)
    {
        if (propertyValue == null)
            return string.Empty;

        var propValueStr = propertyValue.ToString()?.ToUpperInvariant() ?? "null";
        return $"{_entityTypeCacheKey}_{propertyName}_{propValueStr}";
    }

    protected virtual void InsertEntity(string cacheKey, TEntity entity)
        => _cache.Insert(cacheKey, () => entity, TimeSpan.FromMinutes(5), true);

    protected virtual void InsertEntities(TId[]? ids, TEntity[]? entities)
    {
        if (ids?.Length == 0 && entities?.Length == 0 && _options.GetAllCacheAllowZeroCount)
        {
            // getting all of them, and finding nothing.
            // if we can cache a zero count, cache an empty array,
            // for as long as the cache is not cleared (no expiration)
            _cache.Insert(_entityTypeCacheKey, () => _emptyEntities);
        }
        else
        {
            if (entities is not null)
            {
                // individually cache each item
                foreach (TEntity entity in entities)
                {
                    TEntity capture = entity;
                    _cache.Insert(GetEntityCacheKey(entity), () => capture, TimeSpan.FromMinutes(5), true);
                }
            }
        }
    }
}