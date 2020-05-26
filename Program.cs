using System;

// try smaller declarative methods, rollover, do stuff with result

namespace driver_csharp
{
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
			new DataState(tick - StartTick, tick, 11, true, 0);
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
			{
				++numBits;
			}
			return numBits;
		}

		public State bitsToState(bool level, int numBits)
		{
			if (Remaining <= 0)
			{
				Console.WriteLine(Value);
				return new InitState();
			}
			if (numBits <= 0)
			{
				return this;
			}
			if (IgnoreFirst)
			{
				return new DataState(Interval, StartTick, Remaining-1, false, Value).bitsToState(level, numBits-1);
			}
			return new DataState(Interval, StartTick, Remaining-1, false, Value | (level ? 0 : 1) << (Remaining-1)).bitsToState(level, numBits-1);
		}

		public State levelChange(bool level, long tick) =>
			new DataState(Interval, tick, Remaining, IgnoreFirst, Value).bitsToState(level, countBits(tick));
	}

	public static class Transmission
	{
		public static DataState updateValue(this DataState s, long numBits) => s;
		public static DataState updateTimestamp(this DataState s, long tick) => 
			new DataState(s.Interval, tick,s.Remaining, s.IgnoreFirst, s.Value);
		public static DataState transmitValue(this DataState s) {
			Console.WriteLine(s.Value);
			return s;
		}
		public static InitState reinit(this DataState s) => new InitState();
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
