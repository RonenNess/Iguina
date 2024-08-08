using Iguina.Entities;

namespace Iguina.Tests
{
    /// <summary>
    /// Tests for <see cref="NumericInput"/>.
    /// </summary>
    public class NumericInputTests
    {
        [TestCase("5", 5, "5")]
        [TestCase("100", 100, "100")]
        [TestCase("0", 0, "0")]
        [TestCase("005", 5, "5")]
        [TestCase("1.23", 1.23, "1.23")]
        [TestCase("0.23", 0.23, "0.23")]
        //[TestCase(".23", 0.23, ".23")] -- are we allowed to type stuff like ".5"?
        [TestCase("2.", 2, "2.")]
        [TestCase("-5", -5, "-5")]
        [TestCase("-100", -100, "-100")]
        [TestCase("-0", -0, "-0")]
        [TestCase("-005", -5, "-5")]
        [TestCase("-1.23", -1.23, "-1.23")]
        [TestCase("-0.23", -0.23, "-0.23")]
        //[TestCase("-.23", -0.23, "-.23")] -- are we allowed to type stuff like "-.5"?
        [TestCase("-2.", -2, "-2.")]
        [TestCase("1..23", 1.23, "1.23")]
        [TestCase("1.2.3", 1.23, "1.23")]
        [TestCase("1.23.", 1.23, "1.23")]
        public void TestTypeDecimalNumber(string input, decimal expectedValue, string? expectedText = null)
        {
            if (expectedText == null)
                expectedText = input;
            
            var system = new UISystem(new TestRenderer(), new TestInputProvider());
            var numericInput = new NumericInput(system);
            
            // Add each character on at a time, as if the user is typing it
            for (int i = 0; i < input.Length; i++)
                numericInput.Value += input[i];
            
            Assert.That(numericInput.NumericValue, Is.EqualTo(expectedValue));
            Assert.That(numericInput.Value, Is.EqualTo(expectedText));
        }
    }
}