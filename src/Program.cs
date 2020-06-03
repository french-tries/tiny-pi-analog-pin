using System;
using System.Collections.Immutable;

namespace driver_csharp
{
	class Program
	{
		static Action<ImmutableList<int>> printMessage = (ImmutableList<int> values) => 
			Console.WriteLine($"{values[0]} : {values[1]}");

		static void Main(string[] args)
		{
			TxListener transmission =
				new TxListener(printMessage).addData(2).addData(10);

			transmission.start()
				.levelChange(false, 0)
				.levelChange(true, 10) // interval
				.levelChange(false, 20)
				.levelChange(true, 30)
				.levelChange(false, 40) // 1
				.levelChange(true, 130)
				.levelChange(false, 140); // 1

			transmission.start()
				.levelChange(true, 0)
				.levelChange(false, 10) //interval
				.levelChange(true, 130)	// 0
				.levelChange(false, 140); // 1

			transmission.start()
				.levelChange(false, 0)
				.levelChange(true, 10) //interval
				.levelChange(false, 140) // 3 + 1023
				.levelChange(true, 200)
				.levelChange(false, 210)
				.levelChange(true, 330) // 0
				.levelChange(false, 340); // 1
		}
	}
}
