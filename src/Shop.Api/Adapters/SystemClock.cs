using Shop.Application.Common;
namespace Shop.Api.Adapters;

public sealed class SystemClock : IClock { public DateTime UtcNow => DateTime.UtcNow; }
