using System;

// rollover

namespace driver_csharp
{
	public static class Config
	{
		public const int MessageBits = 10;
	}
	public interface State
	{
		State levelChange(bool level, long tick);
	}

	public sealed class InitState : State
	{
		public State levelChange(bool level, long tick) => 
			new IntervalState(tick);
	}

	public sealed class IntervalState : State
	{
		public IntervalState(long tick)
		{
			StartTick = tick;
		}
		public long StartTick { get; }

		public State levelChange(bool level, long tick) => 
			new DataState(tick - StartTick, tick, Config.MessageBits, true, 0);
	}

	public sealed class DataState : State
	{
		public DataState(long interval, long startTick, int remaining, bool ignoreFirst, int value)
		{
			Interval = interval;
			StartTick = startTick;
			Remaining = remaining;
			IgnoreFirst = ignoreFirst;
			Value = value;
		}
		public long Interval { get; }
		public long StartTick { get; }
		public int Remaining { get; }
		public bool IgnoreFirst { get; }
		public int Value { get; }

		public int countBits(long tick)
		{
			int numBits = Convert.ToInt32((tick - StartTick) / Interval);

			if ((tick - StartTick) % Interval > Interval /5*4)
				++numBits;
			if (IgnoreFirst)
				--numBits;

			return numBits;
		}

		public DataState updateTimestamp(long tick) => 
			new DataState(Interval, tick, Remaining, IgnoreFirst, Value);
		public DataState transmitValue() {
			Console.WriteLine(Value);
			return this;
		}
		public InitState reinit() => new InitState();

		public DataState updateValue(int numBits) 
		{
			if (numBits <= 0 || Remaining <= 0)
				return this;

			return new DataState(Interval, StartTick, Remaining-1, false, Value | 1 << (Remaining-1)).updateValue(numBits-1);
		}

		public DataState shiftRemaining(int numBits) => new DataState(Interval, StartTick, Remaining-numBits, false, Value);

		public State levelChange(bool level, long tick)
		{
			var newState = level ?
				updateTimestamp(tick).shiftRemaining(countBits(tick)) :
				updateTimestamp(tick).updateValue(countBits(tick));

			if(newState.Remaining <= 0)
				return newState.transmitValue().reinit();
			else
				return newState;
		}
	}

	class Program
	{
		static void Main(string[] args)
		{
			State state = new InitState();
			state.levelChange(false, 0)
				.levelChange(true, 10)
				.levelChange(false, 30)
				.levelChange(true, 110)
				.levelChange(false, 120); // 513

			State state2 = new InitState();
			state.levelChange(true, 0)
				.levelChange(false, 10)
				.levelChange(true, 110)
				.levelChange(false, 120); // 1
		}
	}
}
