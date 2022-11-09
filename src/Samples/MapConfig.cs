using NgMapper.Helper;

namespace Samples
{
	/// <summary>
	/// We can create independent config class
	/// </summary>
	public class MapConfig : INgCommonMapper
	{
		public void Configure(NgMapCreator config)
		{
			config.ChooseType<SampleDto>(
				type => type.MapTo<SampleSecondDto>(map => 
													map
													 .Init(src => new SampleSecondDto("key"))
													 .Ignore(x => x.Age)
													 .Ignore(x => x.SecondName)
													 .Ignore(x => x.Name)
													 .ForMember(dst => dst.Fullname, (dst, src) => $"{src.Name} {src.SecondName}")));
		}
	}
}