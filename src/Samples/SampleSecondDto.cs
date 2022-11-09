namespace Samples
{
	public class SampleSecondDto
	{
		public SampleSecondDto(string key)
		{
			Key = key;
		}
		public int Id { get; set; }
		public string Fullname { get; set; } = "";
		public string Key { get; set; }
	}
}
