using System;


// skip state
// multiple communications

namespace driver_csharp
{
	public static class Config
	{
		public const int IdBits = 2;
		public const int MessageBits = 10;
	}
	public abstract class State
	{
		protected State(int id = 0) {
			Id = id;
		}

		public int Id { get; }
		public abstract State levelChange(bool rising, long tick);
	}

	public sealed class InitState : State
	{
		public InitState(Func<State, bool, long, State> onSuccess) {
			OnSuccess = onSuccess;
		}

		private Func<State, bool, long, State> OnSuccess { get; }
		public override State levelChange(bool rising, long tick) => 
			OnSuccess(this, rising, tick);
	}

	public sealed class IntervalState : State
	{
		public IntervalState(Func<State, bool, long, State> onSuccess, long tick) : base()
		{
			OnSuccess = onSuccess;
			StartTick = tick;
		}

		private Func<State, bool, long, State> OnSuccess { get; }
		public long StartTick { get; }

		public long getInterval(long tick) => tick - StartTick;

		public override State levelChange(bool rising, long tick) => 
			OnSuccess(this, rising, tick);
	}

	public sealed class DataState : State
	{
		public DataState(Func<State, bool, long, State> onSuccess, 
			int id, long interval, long startTick, int remaining) : 
			this(onSuccess, id, interval, startTick, remaining, true, 0)
		{}
		public DataState(Func<State, bool, long, State> onSuccess, int id,
			long interval, long startTick, int remaining, bool ignoreFirst, int value)  : base(id)
		{
			OnSuccess = onSuccess;
			Interval = interval;
			StartTick = startTick;
			Remaining = remaining;
			IgnoreFirst = ignoreFirst;
			Value = value;
		}

		public DataState With(long? startTick = null,
			int? remaining = null, bool? ignoreFirst = null, int? value = null) =>
			new DataState(OnSuccess, Id, Interval, 
				startTick ?? StartTick, remaining ?? Remaining, false, value ?? Value);

		private Func<State, bool, long, State> OnSuccess { get; }

		public long Interval { get; }
		public long StartTick { get; }
		public int Remaining { get; }
		public bool IgnoreFirst { get; }
		public int Value { get; }

		public long nextTick(long tick) => (tick - StartTick < Interval) ? Interval : (StartTick + Interval);
		public int updateValue(bool rising) {
			if (IgnoreFirst || rising)
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
				if (IgnoreFirst)
					return With(startTick: nextTick(tick), ignoreFirst: false).processBits(rising, tick);
				else
					return With(startTick: nextTick(tick), remaining: Remaining-1,
						value: updateValue(rising)).processBits(rising, tick);
			return this;
		}

		public override State levelChange(bool rising, long tick)
		{
			var newState = processBits(rising, tick);

			if(newState.Remaining <= 0)
				return newState.OnSuccess(newState, rising, tick);
			else
				return newState;
		}
	}

	class Program
	{
		static Func<State, bool, long, State> transitions = (State s, bool rising, long tick) => {
			switch (s){
				case InitState i:
					return new IntervalState(transitions, tick);
				case IntervalState i:
					return new DataState(transitions, 0, i.getInterval(tick), tick, Config.IdBits);
				case DataState d when d.Id == 0:
					Console.WriteLine(d.Value);
					return new DataState(transitions, 1, d.Interval, d.StartTick, Config.MessageBits, false, 0).levelChange(rising, tick);
				case DataState d when d.Id == 1:
					Console.WriteLine(d.Value);
					return new InitState(transitions);
				default:
					Console.WriteLine("error");
					return new InitState(transitions);
			}
		};
		static void Main(string[] args)
		{
			State state = new InitState(transitions);
			state.levelChange(false, 0)
				.levelChange(true, 10) // interval
				.levelChange(false, 20)
				.levelChange(true, 30)
				.levelChange(false, 40) // 1
				.levelChange(true, 130)
				.levelChange(false, 140); // 1

			State state2 = new InitState(transitions);
			state2.levelChange(true, 0)
				.levelChange(false, 10) //interval
				.levelChange(true, 130)	// 0
				.levelChange(false, 140); // 1

			State state3 = new InitState(transitions);
			state3.levelChange(false, 0)
				.levelChange(true, 10) //interval
				.levelChange(false, 140)	// 3
				.levelChange(true, 150); // 1023
		}
	}
}
