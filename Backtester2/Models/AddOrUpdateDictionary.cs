namespace Backtester2.Models
{
	public class AddOrUpdateDictionary<TKey, TValue> : Dictionary<TKey, TValue>
	where TKey : notnull
	{
		public void Update(TKey key, TValue value)
		{
			if (ContainsKey(key))
				this[key] = value;
			else
				Add(key, value);
		}
	}

}
