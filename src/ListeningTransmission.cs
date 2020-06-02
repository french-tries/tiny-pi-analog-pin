using System;
using System.Collections.Immutable;

namespace driver_csharp
{
	public sealed class ListeningTransmission
	{
		public static Func<ListeningTransmission, Func<State, bool, long, State>> transitions = 
			(ListeningTransmission transmission) => (State s, bool rising, long tick) =>
		{
			switch (s){
				case TriggerState t: {
					return new IntervalState(transitions(transmission), tick);
				}
				case IntervalState i: {
					return new DataState(transitions(transmission), i.getInterval(tick), tick, 1, false, 0);
				}
				case DataState d when !transmission.isDone(): {
					(ListeningTransmission next, int count) = transmission.popRemaining();

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

		public ListeningTransmission(Action<ImmutableList<int>> onMessage) : 
			this(onMessage, ImmutableList.Create<int>(), ImmutableList.Create<int>(), ImmutableList.Create<int>())
		{}

		private ListeningTransmission(Action<ImmutableList<int>> onMessage, ImmutableList<int> entries,
			ImmutableList<int> remaining, ImmutableList<int> values)
		{
			OnMessage = onMessage;
			Entries = entries;
			Remaining = remaining;
			Values = values;
		}

		public ListeningTransmission addData(int numbits) => 
			new ListeningTransmission(OnMessage, Entries.Add(numbits), Entries.Add(numbits), Values);

		public State start() =>  new TriggerState(transitions(this));

		private (ListeningTransmission, int) popRemaining() => 
			(new ListeningTransmission(OnMessage, Entries, Remaining.RemoveAt(0), Values), Remaining[0]);

		private ListeningTransmission addValue(int value) =>
			new ListeningTransmission(OnMessage, Entries, Remaining, Values.Add(value));

		private ListeningTransmission finish(int lastValue) {
			OnMessage(Values.Add(lastValue));
			return reset();
		}

		private bool isDone() => Remaining.IsEmpty;

		public ListeningTransmission reset() =>
			new ListeningTransmission(OnMessage, Entries, Entries, ImmutableList.Create<int>());

		private Action<ImmutableList<int>> OnMessage { get; }
		private ImmutableList<int> Entries { get; }
		private ImmutableList<int> Remaining { get; }
		private ImmutableList<int> Values { get; }
	}

}