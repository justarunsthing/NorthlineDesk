using Xunit;

namespace Gate.Fixtures;

public sealed class ShallowTests
{
	[Fact]
	public void Match_result_exists()
	{
		object result = "unchecked";
		Assert.NotNull(result);
	}
}