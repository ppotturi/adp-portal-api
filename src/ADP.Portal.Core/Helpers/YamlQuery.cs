namespace ADP.Portal.Core.Helpers
{
    public class YamlQuery
    {
        private readonly object yamlDictionary;
        private string? key = null;
        private object? current = null;

        public YamlQuery(object yamlDictionary)
        {
            this.yamlDictionary = yamlDictionary;
        }

        public YamlQuery On(string key)
        {
            this.key = key;
            current = Query<object>(current ?? yamlDictionary, this.key, null);
            return this;
        }

        public YamlQuery Get(string prop)
        {
            if (current == null)
                throw new InvalidOperationException();

            current = Query<object>(current, null, prop, key);
            return this;
        }

        public YamlQuery Remove(string prop)
        {
            if (current == null)
                throw new InvalidOperationException();

            Remove<object>(current, prop);
            return this;
        }

        public List<T> ToList<T>()
        {
            if (current == null)
                throw new InvalidOperationException();

            return ((List<object>)current).Cast<T>().ToList();
        }

        private List<T> Query<T>(object instance, string? key, string? prop, string? fromKey = null)
        {
            var result = new List<T>();
            if (instance == null)
                return result;
            if (typeof(IDictionary<object, object>).IsAssignableFrom(instance.GetType()))
            {
                var dictionary = (IDictionary<object, object>)instance;
                var collection = dictionary.Cast<KeyValuePair<object, object>>();

                foreach (var item in collection)
                {
                    if (item.Key as string == key)
                    {
                        if (prop == null)
                        {
                            result.Add((T)item.Value);
                        }
                        else
                        {
                            result.AddRange(Query<T>(item.Value, key, prop, item.Key as string));
                        }
                    }
                    else if (fromKey == key && item.Key as string == prop)
                    {
                        result.Add((T)item.Value);
                    }
                    else
                    {
                        result.AddRange(Query<T>(item.Value, key, prop, item.Key as string));
                    }
                }
            }
            else if (typeof(IEnumerable<object>).IsAssignableFrom(instance.GetType()))
            {
                var collection = (IEnumerable<object>)instance;
                foreach (var item in collection)
                {
                    result.AddRange(Query<T>(item, key, prop, key));
                }
            }
            return result;
        }

        private void Remove<T>(object instance, string? key)
        {
            if (instance == null || string.IsNullOrEmpty(key))
                return;

            if (typeof(IDictionary<object, object>).IsAssignableFrom(instance.GetType()))
            {
                var dictionary = (IDictionary<object, object>)instance;
                dictionary.Remove(key);
            }
            else if (typeof(IEnumerable<object>).IsAssignableFrom(instance.GetType()))
            {
                var collection = (IEnumerable<object>)instance;
                foreach (var item in collection)
                {
                    Remove<T>(item, key);
                }
            }
        }
    }
}
