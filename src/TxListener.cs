using System;
using System.Collections.Immutable;

namespace driver_csharp
{
	public sealed class TxListener
	{
		public static Func<TxListener, Func<State, bool, long, State>> transitions = 
			(TxListener transmission) => (State s, bool rising, long tick) =>
		{
			switch (s){
				case TriggerState t: {
					return new IntervalState(transitions(transmission), tick);
				}
				case IntervalState i: {
					return new DataState(transitions(transmission), i.getInterval(tick), tick, 1, false, 0);
				}
				case DataState d when !transmission.isDone(): {
					(TxListener next, int count) = transmission.popRemaining();

					return new DataState(transitions(d.Keep ? next.addValue(d.Value) : next),
						d.Interval, d.StartTick, count).levelChange(rising, tick);
				}
				case DataState d when transmission.isDone(): {
					return new TriggerState(transitions(transmission.addValue(d.Value).finish(d.Value)));
				}
				case ErrorState e: {
					return e;
				}
				default: {
					Console.WriteLine("error");
					return new TriggerState(transitions(transmission.reset()));
				}
			}
		};

		public TxListener(Action<ImmutableList<int>> onMessage) : 
			this(onMessage, ImmutableList.Create<int>(), ImmutableList.Create<int>(), ImmutableList.Create<int>())
		{}

		private TxListener(Action<ImmutableList<int>> onMessage, ImmutableList<int> entries,
			ImmutableList<int> remaining, ImmutableList<int> values)
		{
			OnMessage = onMessage;
			Entries = entries;
			Remaining = remaining;
			Values = values;
		}

		public TxListener addData(int numbits) => 
			new TxListener(OnMessage, Entries.Add(numbits), Entries.Add(numbits), Values);

		public State start() =>  new TriggerState(transitions(this));

		private (TxListener, int) popRemaining() => 
			(new TxListener(OnMessage, Entries, Remaining.RemoveAt(0), Values), Remaining[0]);

		private TxListener addValue(int value) =>
			new TxListener(OnMessage, Entries, Remaining, Values.Add(value));

		private TxListener finish(int lastValue) {
			OnMessage(Values.Add(lastValue));
			return reset();
		}

		private bool isDone() => Remaining.IsEmpty;

		public TxListener reset() =>
			new TxListener(OnMessage, Entries, Entries, ImmutableList.Create<int>());

		private Action<ImmutableList<int>> OnMessage { get; }
		private ImmutableList<int> Entries { get; }
		private ImmutableList<int> Remaining { get; }
		private ImmutableList<int> Values { get; }
	}

}