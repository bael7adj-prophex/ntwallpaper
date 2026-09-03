namespace NTWallpaper.Infrastructure.Persistence;

using System.Data;
using Dapper;

/// <summary>Registers Dapper type handlers so SQLite (which stores everything as INTEGER/TEXT) maps cleanly.</summary>
public static class SqliteTypeHandlers
{
    private static bool _registered;

    public static void EnsureRegistered()
    {
        if (_registered) return;
        _registered = true;

        SqlMapper.AddTypeHandler(new BoolHandler());
        SqlMapper.AddTypeHandler(new DateTimeHandler());
        SqlMapper.AddTypeHandler(new TimeSpanHandler());
        SqlMapper.AddTypeHandler(new TimeOnlyHandler());
    }

    private sealed class BoolHandler : SqlMapper.TypeHandler<bool>
    {
        public override void SetValue(IDbDataParameter parameter, bool value) => parameter.Value = value ? 1 : 0;
        public override bool Parse(object value) => value switch
        {
            long l => l != 0,
            int i => i != 0,
            bool b => b,
            _ => false
        };
    }

    private sealed class DateTimeHandler : SqlMapper.TypeHandler<DateTime>
    {
        public override void SetValue(IDbDataParameter parameter, DateTime value) => parameter.Value = value.ToUniversalTime().Ticks;
        public override DateTime Parse(object value) => new DateTime(Convert.ToInt64(value), DateTimeKind.Utc);
    }

    private sealed class TimeSpanHandler : SqlMapper.TypeHandler<TimeSpan>
    {
        public override void SetValue(IDbDataParameter parameter, TimeSpan value) => parameter.Value = value.Ticks;
        public override TimeSpan Parse(object value) => new TimeSpan(Convert.ToInt64(value));
    }

    private sealed class TimeOnlyHandler : SqlMapper.TypeHandler<TimeOnly>
    {
        public override void SetValue(IDbDataParameter parameter, TimeOnly value) => parameter.Value = value.Ticks;
        public override TimeOnly Parse(object value) => new TimeOnly(Convert.ToInt64(value));
    }
}
