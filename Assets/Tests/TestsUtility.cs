using System.Collections;
using NUnit.Framework;
using Utility;

namespace Tests
{
	[TestFixture]
	public class TestsUtility
	{
		private static object E => null;
		private static object I { get; } = new();

		private static IEnumerable TestSource_Sort()
		{
			//yield return new TestCaseData(new object[] { null }) { ExpectedResult = new object[] { }, TestName = "sort.null" };
			yield return new TestCaseData(new object[] { new object[] { } }) { ExpectedResult = new object[] { }, TestName = "sort.zero" };
			yield return new TestCaseData(new object[] { new[] { E, E, E, E } }) { ExpectedResult = new[] { E, E, E, E }, TestName = "sort.empty" };
			yield return new TestCaseData(new object[] { new[] { I, I, I, I } }) { ExpectedResult = new[] { I, I, I, I }, TestName = "sort.full" };
			yield return new TestCaseData(new object[] { new[] { I, E, I, E } }) { ExpectedResult = new[] { I, I, E, E }, TestName = "sort.begins.value" };
			yield return new TestCaseData(new object[] { new[] { E, I, I, E } }) { ExpectedResult = new[] { I, I, E, E }, TestName = "sort.begins.null" };
		}

		[TestCaseSource(nameof(TestSource_Sort))]
		public object[] Test_DeFragment(object[] array)
		{
			array.DeFragment();
			array.ToText().Log();
			return array;
		}
	}
}
