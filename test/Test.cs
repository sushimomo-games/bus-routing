using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class Test
{
    [TestCase]
    public void Addition_ShouldBeCorrect()
    {
        int result = 2 + 2;

        AssertThat(result).IsEqual(4);
    }
}