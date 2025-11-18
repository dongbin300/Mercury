namespace Vectoris.Charts.Core;

[AttributeUsage(AttributeTargets.Class)]
public class IndicatorAttribute(string name) : Attribute
{
	public string Name { get; } = name;
}
