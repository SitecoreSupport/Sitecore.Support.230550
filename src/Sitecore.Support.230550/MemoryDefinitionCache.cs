namespace Sitecore.Support.Xdb.ReferenceData.Service.Cache
{

  using Microsoft.Extensions.Configuration;
  using Newtonsoft.Json;
  using Newtonsoft.Json.Serialization;
  using Sitecore.Framework.Conditions;
  using Sitecore.Support;
  using Sitecore.Xdb.ReferenceData.Core;
  using Sitecore.Xdb.ReferenceData.Core.Cache;
  using System;
  using System.Globalization;
  using System.Runtime.Caching;
  using System.Runtime.Serialization.Formatters;
  using System.Threading;

  public class MemoryDefinitionCache : IDefinitionCache, IDisposable
  {
    public const string EntryLifetimeConfigurationKey = "EntryLifetime";

    public const string EntryLifetimeConfigurationDefaultValue = "00:00:30";

    #region Added Code
    private readonly JsonSerializerSettings _settings;
    #endregion

    protected MemoryCache Cache
    {
      get;
    }

    protected TimeSpan EntryLifetime
    {
      get;
      set;
    }

    protected ReaderWriterLockSlim CacheLock
    {
      get;
    }

    public MemoryDefinitionCache(string entryLifetime)
        : this(TimeSpan.Parse(entryLifetime, CultureInfo.CurrentCulture))
    {
    }

    public MemoryDefinitionCache(TimeSpan entryLifetime)
    {
      EntryLifetime = entryLifetime;
      Cache = new MemoryCache("DefinitionCache", null);
      CacheLock = new ReaderWriterLockSlim();
      #region Added Code
      _settings = new JsonSerializerSettings
      {
        TypeNameHandling = TypeNameHandling.All,
        Formatting = Formatting.None,
        TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
        ContractResolver = new DefaultContractResolver
        {
          IgnoreSerializableInterface = true
        }
      };
      #endregion
    }

    public MemoryDefinitionCache(IConfiguration configuration)
        : this(Condition.Requires(configuration, "configuration").IsNotNull().Value.GetValue("EntryLifetime", "00:00:30"))
    {
    }

    public void Add<TDefinition>(string key, TDefinition definition) where TDefinition : BaseDefinition
    {
      #region Modified Code
      //Original this.AddObject(key,definition) 
      Condition.Requires(definition, "definition").IsNotNull();
      string valueToCache = JsonConvert.SerializeObject(definition, _settings);
      AddObject(key, valueToCache);
      #endregion
    }

    public void Add<TDefinition>(string key, ResultSet<TDefinition> resultSet) where TDefinition : BaseDefinition
    {
      AddObject(key, resultSet);
    }

    public void Add(string key, DefinitionTypeKey typeKey)
    {
      AddObject(key, typeKey);
    }

    public TDefinition GetDefinition<TDefinition>(string key) where TDefinition : BaseDefinition
    {
      Condition.Requires(key, "key").IsNotNull();
      CacheLock.EnterReadLock();
      try
      {
        object obj = Cache.Get(key, null);
        if (obj != null)
        {
          return JsonConvert.DeserializeObject<TDefinition>((string)obj, _settings);
        }
        return null;
      }
      finally
      {
        CacheLock.ExitReadLock();
      }
    }

    public ResultSet<TDefinition> GetResultSet<TDefinition>(string key) where TDefinition : BaseDefinition
    {
      Condition.Requires(key, "key").IsNotNull();
      CacheLock.EnterReadLock();
      try
      {
        object obj = Cache.Get(key, null);
        if (obj != null)
        {
          ResultSet<TDefinition> obj2 = obj as ResultSet<TDefinition>;
          Condition.Requires(obj2, "resultSet").IsNotNull(Resources.CacheEntryInvalidType);
          return obj2;
        }
        return null;
      }
      finally
      {
        CacheLock.ExitReadLock();
      }
    }

    public DefinitionTypeKey GetDefinitionType(string key)
    {
      Condition.Requires(key, "key").IsNotNull();
      CacheLock.EnterReadLock();
      try
      {
        object obj = Cache.Get(key, null);
        if (obj != null)
        {
          DefinitionTypeKey obj2 = obj as DefinitionTypeKey;
          Condition.Requires(obj2, "typeKey").IsNotNull(Resources.CacheEntryInvalidType);
          return obj2;
        }
        return null;
      }
      finally
      {
        CacheLock.ExitReadLock();
      }
    }

    public void Clear()
    {
      CacheLock.EnterWriteLock();
      try
      {
        Cache.Trim(100);
      }
      finally
      {
        CacheLock.ExitWriteLock();
      }
    }

    public bool Remove(string key)
    {
      Condition.Requires(key, "key").IsNotNull();
      if (CacheLock.TryEnterWriteLock(TimeSpan.FromMilliseconds(500.0)))
      {
        try
        {
          return Cache.Remove(key, null) != null;
        }
        finally
        {
          CacheLock.ExitWriteLock();
        }
      }
      return false;
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    #region Removed Code
    //[SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId="<CacheLock>k__BackingField", Justification="MS recommended pattern"), SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId="<Cache>k__BackingField", Justification="MS recommended pattern")]
    #endregion
    protected virtual void Dispose(bool disposing)
    {
      if (disposing)
      {
        Cache.Dispose();
        CacheLock.Dispose();
      }
    }

    protected void AddObject(string key, object valueToCache)
    {
      Condition.Requires(key, "key").IsNotNull();
      Condition.Requires(valueToCache, "valueToCache").IsNotNull();
      DateTime dateTime = DateTime.Now.Add(EntryLifetime);
      CacheLock.EnterWriteLock();
      try
      {
        if (Cache.Contains(key, null))
        {
          Cache.Remove(key, null);
        }
        Cache.Add(key, valueToCache, dateTime, null);
      }
      finally
      {
        CacheLock.ExitWriteLock();
      }
    }
  }

}