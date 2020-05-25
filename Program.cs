using System;

namespace driver_csharp
{
	public interface State
	{
		State apply(bool level, long tick);
	}

	public class InitState : State
	{
		public InitState()
		{
			Console.WriteLine("new InitState");
		}
		public State apply(bool level, long tick)
		{
			return new IntervalState(tick);
		}
	}

	public class IntervalState : State
	{
		public IntervalState(long tick) 
		{
			Console.WriteLine("new IntervalState @ " + tick);
			StartTick = tick;
		}
		private long StartTick { get; }
		public State apply(bool level, long tick)
		{
			return new DataState(tick - StartTick, tick, 11, true, 0);
		}
	}

	public class DataState : State
	{
		public DataState(long interval, long startTick, int remaining, bool ignoreFirst, int value) 
		{
			Console.WriteLine("new DataState @ " + startTick + " with " + remaining);

			Interval = interval;
			StartTick = startTick;
			Remaining = remaining;
			IgnoreFirst = ignoreFirst;
			Value = value;
		}
		private long Interval { get; }
		private long StartTick { get; }
		private int Remaining { get; }
		private bool IgnoreFirst { get; }
		private int Value { get; }

		private int countBits(long tick)
		{
			int numBits = Convert.ToInt32((tick - StartTick) / Interval);

			if ((tick - StartTick) % Interval > Interval /5*4)
			{
				++numBits;
			}
			return numBits;
		}

		private int bitsToValue(int remaining, int numBits, int startValue)
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

		public State apply(bool level, long tick)
		{
			int numBits = countBits(tick);
			int newValue = level ? Value : bitsToValue(
				IgnoreFirst ? Remaining-1 : Remaining, IgnoreFirst ? numBits -1 : numBits, Value);

			if (numBits < Remaining)
			{
				return new DataState(Interval, tick, Remaining - numBits, false, newValue);
			}
			else
			{
				Console.WriteLine(newValue);
				return new InitState();
			}
		}
	}

	class Program
	{
		static void Main(string[] args)
		{
			State state = new InitState();
			state.apply(false, 0)
				.apply(true, 10)
				.apply(false, 30)
				.apply(true, 110)
				.apply(false, 120); // 513

			State state2 = new InitState();
			state2.apply(true, 0)
				.apply(false, 10)
				.apply(true, 110)
				.apply(false, 120); // 1
		}
	}
}
