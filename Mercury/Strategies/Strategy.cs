﻿using Mercury.Interfaces;
using Mercury.Orders;
using Mercury.Signals;

namespace Mercury.Strategies
{
	public class Strategy : IStrategy
	{
		public string Name { get; set; } = string.Empty;
		public ICue? Cue { get; set; } = null;
		public ISignal Signal { get; set; } = new Signal();
		public IOrder Order { get; set; } = new Order();
		public string Tag { get; set; } = string.Empty;

		public Strategy()
		{

		}

		public Strategy(string name) : this()
		{
			Name = name;
		}

		public Strategy(string name, ISignal signal) : this(name)
		{
			Signal = signal;
		}

		public Strategy(string name, IOrder order) : this(name)
		{
			Order = order;
		}

		public Strategy(string name, ICue cue) : this(name)
		{
			Cue = cue;
		}

		public Strategy(string name, string tag) : this(name)
		{
			Tag = tag;
		}

		public Strategy(string name, ISignal signal, IOrder order) : this(name)
		{
			Signal = signal;
			Order = order;
		}

		public Strategy(string name, ICue? cue, ISignal signal, IOrder order) : this(name, signal, order)
		{
			Cue = cue;
		}
	}
}
