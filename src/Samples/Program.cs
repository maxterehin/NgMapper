namespace Samples
{
	public class Program
	{
		public static void Main()
		{
			var dto = new SampleDto();
			dto.Name = "Dto";

			var entity = dto.MapToSampleEntity();
			//test.NameA = "Prop in test";
			//var bClass = test.MapToB();
			Console.WriteLine("Hello World");
		}
	}
}
