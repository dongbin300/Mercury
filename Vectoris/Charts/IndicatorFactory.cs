using System.Reflection;

using Vectoris.Charts.Core;

namespace Vectoris.Charts;

public static class IndicatorFactory
{
	private static readonly Dictionary<string, Type> _registry = new(StringComparer.OrdinalIgnoreCase);

	static IndicatorFactory()
	{
		var types = Assembly.GetExecutingAssembly()
			.GetTypes()
			.Where(t => t.IsClass && !t.IsAbstract && typeof(IndicatorBase).IsAssignableFrom(t));

		foreach (var type in types)
		{
			var attr = type.GetCustomAttribute<IndicatorAttribute>();
			if (attr != null)
				_registry[attr.Name] = type;
		}
	}

	/// <summary>
	/// 이름과 생성자 파라미터로 지표 인스턴스 생성
	/// </summary>
	/// <param name="name">지표 이름</param>
	/// <param name="args">생성자 파라미터 (EMA: period, MACD: fast, slow, signal 등)</param>
	/// <returns>IndicatorBase 인스턴스</returns>
	public static IndicatorBase Get(string name, params object[] args)
	{
		if (!_registry.TryGetValue(name, out Type? type))
			throw new ArgumentException($"Indicator '{name}' is not registered.");

		try
		{
			return (IndicatorBase)Activator.CreateInstance(type, args)!;
		}
		catch (MissingMethodException)
		{
			throw new ArgumentException($"Indicator '{name}' does not have a matching constructor for provided arguments.");
		}
	}
}
