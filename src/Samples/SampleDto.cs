using NgMapper.Helper;

namespace Samples
{
	/// <summary>
	/// We can mark file as mappable and describe map config
	/// </summary>
	public class SampleDto : INgMapper<SampleDto>
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string SecondName { get; set; }
		public int Age { get; set; }

		public void Configure(NgMapConfig<SampleDto> config)
		{
			//We can ignore some property
			config.MapTo<SampleEntity>(map =>
				map
					.Ignore(x => x.Age)
					.Ignore(x => x.SecondName));

			//Also we can make custom method to map propery
			config.MapFrom<SampleEntity>(map =>
				map
					.ForMember(dst => dst.Age, (dst, src) => src.Name.Length));
		}
	}
}

//After described configs (SampleDto, MapConfig) will be generate this code

//using System;
//namespace Samples
//{
//	public static class SampleDtoNgMapperExtensions
//	{
//		public static SampleSecondDto MapToSampleSecondDto(this SampleDto source)
//		{
//			Func<SampleDto, SampleSecondDto> constructorCreator = src => new SampleSecondDto("key");
//			var result = constructorCreator(source);
//			result.Id = source.Id;
//			Func<SampleSecondDto, SampleDto, String> FullnameLambda = (dst, src) => $"{src.Name} {src.SecondName}";
//			result.Fullname = FullnameLambda(result, source);

//			return result;
//		}
//		public static SampleEntity MapToSampleEntity(this SampleDto source)
//		{
//			var result = new SampleEntity();
//			result.Id = source.Id;
//			result.Name = source.Name;

//			return result;
//		}
//	}
//}
//namespace Samples
//{
//	public static class SampleEntityNgMapperExtensions
//	{
//		public static SampleDto MapToSampleDto(this SampleEntity source)
//		{
//			var result = new SampleDto();
//			result.Id = source.Id;
//			result.Name = source.Name;
//			Func<SampleDto, SampleEntity, Int32> AgeLambda = (dst, src) => src.Name.Length;
//			result.Age = AgeLambda(result, source);

//			return result;
//		}
//	}
//}
