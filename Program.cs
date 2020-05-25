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
			return new DumpState(tick - StartTick, tick);
		}
	}

	public class DumpState : State
	{
		public DumpState(long interval, long tick) 
		{
			Console.WriteLine("new DumpState @ " + tick);
			Interval = interval;
			StartTick = tick;
		}
		private long Interval { get; }
		private long StartTick { get; }
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

			if (numBits <= 10)
			{
				return new DataState(Interval, tick, 11 - numBits, !level ? bitsToValue(10, numBits-1, 0) : 0);
			}
			else
			{
				Console.WriteLine(!level ? bitsToValue(10, numBits, 0) : 0);
				return new InitState();
			}
		}
	}

	public class DataState : State
	{
		public DataState(long interval, long startTick, int remaining, int value) 
		{
			Console.WriteLine("new DataState @ " + startTick + " with " + remaining);

			Interval = interval;
			StartTick = startTick;
			Remaining = remaining;
			Value = value;
		}
		private long Interval { get; }
		private long StartTick { get; }
		private int Remaining { get; }
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

			if (numBits < Remaining)
			{
				return new DataState(Interval, tick, Remaining - numBits, !level ? bitsToValue(Remaining, numBits, Value) : Value);
			}
			else
			{
				Console.WriteLine(!level ? bitsToValue(Remaining, numBits, Value) : Value);
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
