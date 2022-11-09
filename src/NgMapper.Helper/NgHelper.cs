using System;

namespace NgMapper.Helper
{
    public interface INgMapper<TSource>
    {
        void Configure(NgMapConfig<TSource> config);
    }

    public interface INgCommonMapper
    {
        void Configure(NgMapCreator config);
    }

    public sealed class NgMapSetting<TSource, TTarget>
    {
        public NgMapSetting<TSource, TTarget> Ignore<P>(Func<TSource, P> property) { return this; }
        public NgMapSetting<TSource, TTarget> Init(Func<TSource, TTarget> ctor) { return this; }
        public NgMapSetting<TSource, TTarget> ForMember<P>(Func<TTarget, P> property, Func<TTarget, TSource, P> value) { return this; }
    }

    public sealed class NgMapConfig<TSource>
    {
        public NgMapConfig<TSource> MapTo<TTarget>(Action<NgMapSetting<TSource, TTarget>> config)
        {
            return new();
        }

        public NgMapConfig<TSource> MapFrom<Source>(Action<NgMapSetting<Source, TSource>> config)
        {
            return new();
        }
    }

    public sealed class NgMapCreator
    {
        public NgMapCreator ChooseType<TSource>(Action<NgMapConfig<TSource>> config)
        {
            return this;
        }
    }
}
