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
        [TestCase("00", 0, "0")]
        [TestCase("000", 0, "0")]
        [TestCase("05", 5, "5")]
        [TestCase("005", 5, "5")]
        [TestCase("00500", 500, "500")]
        [TestCase("1.23", 1.23, "1.23")]
        [TestCase("0.23", 0.23, "0.23")]
        [TestCase("0.04", 0.04, "0.04")]
        [TestCase("0.004", 0.004, "0.004")]
        [TestCase("00.2", 0.2, "0.2")]
        [TestCase("000.2", 0.2, "0.2")]
        [TestCase("23.0", 23.0, "23.0")]
        [TestCase("23.00", 23.0, "23.00")]
        [TestCase("23.000", 23.0, "23.000")]
        [TestCase("02.3", 2.3, "2.3")]
        [TestCase("20.3", 20.3, "20.3")]
        [TestCase("0020.3", 20.3, "20.3")]
        [TestCase(".23", 0.23, "0.23")]
        [TestCase("2.", 2, "2.")]
        [TestCase("-5", -5, "-5")]
        [TestCase("-100", -100, "-100")]
        [TestCase("-0", -0, "-0")]
        [TestCase("-00", -0, "-0")]
        [TestCase("-000", -0, "-0")]
        [TestCase("-05", -5, "-5")]
        [TestCase("-005", -5, "-5")]
        [TestCase("-00500", -500, "-500")]
        [TestCase("-1.23", -1.23, "-1.23")]
        [TestCase("-0.23", -0.23, "-0.23")]
        [TestCase("-0.04", -0.04, "-0.04")]
        [TestCase("-0.004", -0.004, "-0.004")]
        [TestCase("-00.2", -0.2, "-0.2")]
        [TestCase("-000.2", -0.2, "-0.2")]
        [TestCase("-23.0", -23.0, "-23.0")]
        [TestCase("-23.00", -23.0, "-23.00")]
        [TestCase("-23.000", -23.0, "-23.000")]
        [TestCase("-0023.0", -23.0, "-23.0")]
        [TestCase("-.23", -0.23, "-0.23")]
        [TestCase("-2.", -2, "-2.")]
        [TestCase("-", 0, "-")]
        [TestCase("0.", 0, "0.")]
        [TestCase("0.0", 0, "0.0")]
        [TestCase("0.000", 0, "0.000")]
        [TestCase("00.0", 0, "0.0")]
        [TestCase("000.0", 0, "0.0")]
        [TestCase("000.000", 0, "0.000")]
        [TestCase(".", 0, "0.")]
        [TestCase(".0", 0, "0.0")]
        [TestCase(".000", 0, "0.000")]
        [TestCase("-0.", 0, "-0.")]
        [TestCase("-0.0", 0, "-0.0")]
        [TestCase("-0.000", 0, "-0.000")]
        [TestCase("-00.0", 0, "-0.0")]
        [TestCase("-000.0", 0, "-0.0")]
        [TestCase("-000.000", 0, "-0.000")]
        [TestCase("-.", 0, "-0.")]
        [TestCase("-.0", 0, "-0.0")]
        [TestCase("-.000", 0, "-0.000")]
        [TestCase("", 0, "")]
        [TestCase("x", 0, "")]
        public void TestEnterDecimalNumber(string input, decimal expectedValue, string? expectedText = null)
        {
            if (expectedText == null)
                expectedText = input;
            
            var system = new UISystem(new TestRenderer(), new TestInputProvider());

            var numericInput = new NumericInput(system);

            // Add each character on at a time, as if the user is typing it
            
            for (int i = 0; i < input.Length; i++)
                numericInput.Value += input[i];
            
            Assert.That(numericInput.NumericValue, Is.EqualTo(expectedValue), "Typed value mismatch");
            Assert.That(numericInput.Value, Is.EqualTo(expectedText), "Typed string mismatch");
            
            // Add the whole text at once
            
            numericInput = new NumericInput(system);

            numericInput.Value = input;
            
            Assert.That(numericInput.NumericValue, Is.EqualTo(expectedValue), "Set value mismatch");
            Assert.That(numericInput.Value, Is.EqualTo(expectedText), "Set string mismatch");
        }
        
        // todo: integer tests, i.e. not accepting decimal
    }
}