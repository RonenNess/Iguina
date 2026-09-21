using Iguina.Defs;

namespace Iguina.Tests
{
    /// <summary>
    /// Tests for <see cref="StyleSheet"/>.
    /// </summary>
    public class StyleSheetTests
    {
        [Test]
        public void InheritFromIncludesAllStates()
        {
            var folder = Path.Combine(Path.GetTempPath(), "IguinaTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                var stateNames = new[] { "Default", "Targeted", "Focused", "Interacted", "Checked", "TargetedChecked", "Disabled", "DisabledChecked" };
                var parentJson = "{" + string.Join(",", stateNames.Select((name, i) => $"\"{name}\": {{\"FontSize\": {i + 10}}}")) + "}";
                File.WriteAllText(Path.Combine(folder, "parent.json"), parentJson);
                File.WriteAllText(Path.Combine(folder, "child.json"), "{\"InheritFrom\": \"parent.json\"}");

                var stylesheet = StyleSheet.LoadFromJsonFile(Path.Combine(folder, "child.json"));

                for (int i = 0; i < stateNames.Length; ++i)
                {
                    var state = Enum.Parse<EntityState>(stateNames[i]);
                    Assert.That(stylesheet.GetStyle(state).FontSize, Is.EqualTo(i + 10), $"State '{stateNames[i]}' was not inherited.");
                }
            }
            finally
            {
                Directory.Delete(folder, true);
            }
        }
    }
}
