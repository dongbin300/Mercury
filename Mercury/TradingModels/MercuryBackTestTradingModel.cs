﻿using Binance.Net.Enums;

using Mercury.Elements;
using Mercury.Interfaces;
using Mercury.Scenarios;
using Mercury.Strategies;

namespace Mercury.TradingModels
{
	public class MercuryBackTestTradingModel
	{
		public decimal Asset { get; set; }
		public DateTime StartTime { get; set; }
		public TimeSpan Period { get; set; }
		public KlineInterval Interval { get; set; }
		public IList<string> Targets { get; set; } = [];
		public IList<ChartElement> ChartElements { get; set; } = [];
		public IList<NamedElement> NamedElements { get; set; } = [];
		public IList<IScenario> Scenarios { get; set; } = [];

		public MercuryBackTestTradingModel()
		{

		}

		public void AddCue(string scenarioName, string strategyName, ICue cue)
		{
			var scenario = Scenarios.FirstOrDefault(s => s.Name.Equals(scenarioName));
			if (scenario == null)
			{
				Scenarios.Add(
				new Scenario(scenarioName)
					.AddStrategy(new Strategy(strategyName, cue))
					);
				return;
			}

			var strategy = scenario.Strategies.FirstOrDefault(s => s.Name.Equals(strategyName));
			if (strategy == null)
			{
				scenario.AddStrategy(new Strategy(strategyName, cue));
				return;
			}

			strategy.Cue = cue;
		}

		public void AddSignal(string scenarioName, string strategyName, ISignal signal)
		{
			var scenario = Scenarios.FirstOrDefault(s => s.Name.Equals(scenarioName));
			if (scenario == null)
			{
				Scenarios.Add(
					new Scenario(scenarioName)
					.AddStrategy(new Strategy(strategyName, signal))
					);
				return;
			}

			var strategy = scenario.Strategies.FirstOrDefault(s => s.Name.Equals(strategyName));
			if (strategy == null)
			{
				scenario.AddStrategy(new Strategy(strategyName, signal));
				return;
			}

			strategy.Signal = signal;
		}

		public void AddOrder(string scenarioName, string strategyName, IOrder order)
		{
			var scenario = Scenarios.FirstOrDefault(s => s.Name.Equals(scenarioName));
			if (scenario == null)
			{
				Scenarios.Add(
				new Scenario(scenarioName)
				   .AddStrategy(new Strategy(strategyName, order))
				   );
				return;
			}

			var strategy = scenario.Strategies.FirstOrDefault(s => s.Name.Equals(strategyName));
			if (strategy == null)
			{
				scenario.AddStrategy(new Strategy(strategyName, order));
				return;
			}

			strategy.Order = order;
		}

		public void AddTag(string scenarioName, string strategyName, string tag)
		{
			var scenario = Scenarios.FirstOrDefault(s => s.Name.Equals(scenarioName));
			if (scenario == null)
			{
				Scenarios.Add(
				new Scenario(scenarioName)
				   .AddStrategy(new Strategy(strategyName, tag))
				   );
				return;
			}

			var strategy = scenario.Strategies.FirstOrDefault(s => s.Name.Equals(strategyName));
			if (strategy == null)
			{
				scenario.AddStrategy(new Strategy(strategyName, tag));
				return;
			}

			strategy.Tag = tag;
		}

		public string AddNamedElement(string name, string parameterString)
		{
			if (NamedElements.Any(x => x.Name.Equals(name)))
			{
				return "이미 존재하는 이름입니다.";
			}

			NamedElements.Add(new NamedElement(name, parameterString));
			return string.Empty;
		}

		public bool AnyNamedElement(string name) => NamedElements.Any(x => x.Name.Equals(name));
		public NamedElement? GetNamedElement(string name) => NamedElements.FirstOrDefault(x => x.Name.Equals(name));
	}
}
