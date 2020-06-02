using System;

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
}
