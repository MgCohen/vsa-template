namespace CheckModel.Tests;

using Umpire.CheckModel;

public sealed class DefinitionTextTests {
    [Test]
    public void A_definition_is_the_same_text_after_a_read() {
        var text = Samples.Layers.Write();

        Assert.That(Definition.Read(text).Write(), Is.EqualTo(text));
    }

    [Test]
    public void The_text_has_no_rule_text_and_no_ID() {
        var text = Samples.Layers.Write();

        Assert.That(text, Does.Not.Contain("Ui uses").And.Not.Contain("\"id\""));
    }

    [Test]
    public void A_rule_gives_its_text() =>
        Assert.That(Samples.Layers.Uses[0].ToString(), Is.EqualTo("Ui uses Data"));

    [TestCase("\"format\":1", "\"format\":9")]
    [TestCase("\"children\"", "\"pieces\"")]
    [TestCase("\"parts\"", "\"pieces\"")]
    [TestCase("\"Deny\"", "\"Never\"")]
    public void A_text_of_another_shape_fails(string name, string replacement) {
        var text = Samples.Layers with { Parts = [Samples.Part(Samples.Ui, "Ui", Samples.Part(Samples.Data, "Data"))] };

        Assert.That(() => Definition.Read(text.Write().Replace(name, replacement)), Throws.TypeOf<FormatException>());
    }

    [Test]
    public void A_field_that_is_missing_fails() =>
        Assert.That(() => Definition.Read("{\"format\":1}"), Throws.TypeOf<FormatException>().With.Message.Contains("name"));

    [TestCase("")]
    [TestCase("{")]
    [TestCase("{\"format\":1,}")]
    [TestCase("{\"format\":1} x")]
    [TestCase("{\"format\":\"1\"}")]
    [TestCase("{\"format\":{}}")]
    [TestCase("{\"format\":1,\"name\":{\"a\":1}}")]
    public void A_text_that_is_not_the_JSON_of_a_definition_fails(string text) =>
        Assert.That(() => Definition.Read(text), Throws.TypeOf<FormatException>());

    [Test]
    public void A_name_with_special_characters_is_the_same_after_a_read() {
        var definition = Samples.Layers with { Name = "a \"b\" \\ \n\t\u0001 é" };

        Assert.That(Definition.Read(definition.Write()).Name, Is.EqualTo(definition.Name));
    }
}
