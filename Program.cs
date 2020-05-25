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
		public static int countBits(long startTick, long tick, long interval)
		{
			int numBits = Convert.ToInt32((tick - startTick) / interval);

			if ((tick - startTick) % interval > interval /5*4)
			{
				++numBits;
			}
			return numBits;
		}

		public static int bitsToValue(int remaining, int numBits, int startValue)
		{
			if (remaining <= 0 || numBits <= 0)
			{
				return startValue;
			}
			else
			{
				return bitsToValue(remaining-1, numBits-1, startValue | 1 << (remaining-1));
			}
		}

		public static State levelChange(this State currentState, bool level, long tick)
		{
			switch (currentState)
			{
				case InitState s:
					return new IntervalState(tick);
				case IntervalState s:
					return new DataState(tick - s.StartTick, tick, 11, true, 0);
				case DataState s:
					int numBits = countBits(s.StartTick, tick, s.Interval);
					int newValue = level ? s.Value : bitsToValue(
						s.IgnoreFirst ? s.Remaining-1 : s.Remaining, s.IgnoreFirst ? numBits -1 : numBits, s.Value);

					if (numBits < s.Remaining)
					{
						return new DataState(s.Interval, tick, s.Remaining - numBits, false, newValue);
					}
					else
					{
						Console.WriteLine(newValue);
						return new InitState();
					}
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
