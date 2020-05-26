using System;

namespace driver_csharp
{
	public interface State {}

	public sealed class InitState : State {}

	public sealed class IntervalState : State
	{
		public IntervalState(long tick)
		{
			StartTick = tick;
		}
		public long StartTick { get; }
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
	}

	public static class Transmission
	{
		public static int countBits(DataState s, long tick)
		{
			int numBits = Convert.ToInt32((tick - s.StartTick) / s.Interval);

			if ((tick - s.StartTick) % s.Interval > s.Interval /5*4)
			{
				++numBits;
			}
			return numBits;
		}

		public static State bitsToState(DataState s, bool level, int numBits)
		{
			if (s.Remaining <= 0)
			{
				Console.WriteLine(s.Value);
				return new InitState();
			}
			if (numBits <= 0)
			{
				return s;
			}
			if (s.IgnoreFirst)
			{
				return bitsToState(
					new DataState(s.Interval, s.StartTick, s.Remaining-1, false, s.Value),
					level, numBits-1);
			}
			return bitsToState(
				new DataState(s.Interval, s.StartTick, s.Remaining-1, false, s.Value | (level ? 0 : 1) << (s.Remaining-1)),
				level, numBits-1);
		}

		public static State levelChange(this InitState state, bool level, long tick) => 
			new IntervalState(tick);

		public static State levelChange(this IntervalState s, bool level, long tick) => 
			new DataState(tick - s.StartTick, tick, 11, true, 0);

		public static State levelChange(this DataState s, bool level, long tick) => bitsToState(
				new DataState(s.Interval, tick, s.Remaining, s.IgnoreFirst, s.Value), level, countBits(s, tick));

		// looks like poor man's polymorphism...
		public static State levelChange(this State currentState, bool level, long tick)
		{
			switch (currentState)
			{
				case InitState s:
					return levelChange(s, level, tick);
				case IntervalState s:
					return levelChange(s, level, tick);
				case DataState s:
					return levelChange(s, level, tick);
				default:
					throw new ArgumentException(
						message: "currentState is not a recognized state",
						paramName: nameof(currentState));
			}
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
