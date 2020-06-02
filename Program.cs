using System;
using System.Collections.Immutable;

// testing
// use concrete gpio
// use all possible tiny pins
// ask for value
// parity
// error state when data changes at the wrong time
// reset after error
// version with fixed clock rate to see how simpler it would be

namespace driver_csharp
{
	public interface State
	{
		State levelChange(bool rising, long tick);
	}

	public sealed class TriggerState : State
	{
		public TriggerState(Func<State, bool, long, State> onSuccess, int id = 0) {
			OnSuccess = onSuccess;
		}

		private Func<State, bool, long, State> OnSuccess { get; }
		public State levelChange(bool rising, long tick) => 
			OnSuccess(this, rising, tick);
	}

	public sealed class ErrorState : State
	{
		public ErrorState(Func<State, bool, long, State> onSuccess, int id = 0) {
			OnSuccess = onSuccess;
		}

		private Func<State, bool, long, State> OnSuccess { get; }
		public State levelChange(bool rising, long tick) => this;
	}

	public sealed class IntervalState : State
	{
		public IntervalState(Func<State, bool, long, State> onSuccess, long tick)
		{
			OnSuccess = onSuccess;
			StartTick = tick;
		}

		private Func<State, bool, long, State> OnSuccess { get; }
		public long StartTick { get; }

		public long getInterval(long tick) => tick - StartTick;

		public State levelChange(bool rising, long tick) => 
			OnSuccess(this, rising, tick);
	}

	public sealed class DataState : State
	{
		public DataState(Func<State, bool, long, State> onSuccess, 
			long interval, long startTick, int remaining) : 
			this(onSuccess, interval, startTick, remaining, true, 0)
		{}
		public DataState(Func<State, bool, long, State> onSuccess,
			long interval, long startTick, int remaining, bool keep, int value)
		{
			OnSuccess = onSuccess;
			Interval = interval;
			StartTick = startTick;
			Remaining = remaining;
			Keep = keep;
			Value = value;
		}

		public DataState With(long? startTick = null,
			int? remaining = null, bool? keep = null, int? value = null) =>
			new DataState(OnSuccess, Interval, 
				startTick ?? StartTick, remaining ?? Remaining, keep ?? Keep, value ?? Value);

		private Func<State, bool, long, State> OnSuccess { get; }

		public long Interval { get; }
		public long StartTick { get; }
		public int Remaining { get; }
		public bool Keep { get; }
		public int Value { get; }

		public long nextTick(long tick) => (tick - StartTick < Interval) ? Interval : (StartTick + Interval);
		public int updateValue(bool rising) {
			if (rising)
				return Value;
			return Value | 1 << (Remaining-1);
		}

		public DataState processBits(bool rising, long tick) 
		{
			if (tick - StartTick < Interval/5)
				return With(startTick: tick);
			if (Remaining <= 0)
				return this;
			if (tick - StartTick > Interval/5*4)
				return With(startTick: nextTick(tick), remaining: Remaining-1,
					value: updateValue(rising)).processBits(rising, tick);
			return this;
		}

		public State levelChange(bool rising, long tick)
		{
			var newState = processBits(rising, tick);

			if(newState.Remaining <= 0)
				return newState.OnSuccess(newState, rising, tick);
			else
				return newState;
		}
	}

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

	class Program
	{
		static Action<ImmutableList<int>> printMessage = (ImmutableList<int> values) => 
			Console.WriteLine($"{values[0]} : {values[1]}");

		static void Main(string[] args)
		{
			ListeningTransmission transmission =
				new ListeningTransmission(printMessage).addData(2).addData(10);

			State state = transmission.start();
			state.levelChange(false, 0)
				.levelChange(true, 10) // interval
				.levelChange(false, 20)
				.levelChange(true, 30)
				.levelChange(false, 40) // 1
				.levelChange(true, 130)
				.levelChange(false, 140); // 1

			State state2 = transmission.start();
			state2.levelChange(true, 0)
				.levelChange(false, 10) //interval
				.levelChange(true, 130)	// 0
				.levelChange(false, 140); // 1

			State state3 = transmission.start();
			state3.levelChange(false, 0)
				.levelChange(true, 10) //interval
				.levelChange(false, 140) // 3 + 1023
				.levelChange(true, 200)
				.levelChange(false, 210)
				.levelChange(true, 330) // 0
				.levelChange(false, 340); // 1
		}
	}
}
