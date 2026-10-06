using Newtonsoft.Json.Linq;

using NUnit.Framework;

using Translation.Providers.AI;

namespace Translation.Tests.Providers
{
    /// <summary>
    /// A low temperature keeps a translation from wandering, and every model
    /// used to take one. OpenAI's reasoning models take only their own default
    /// and refuse a request naming any other, so a player who typed one of
    /// them in got nothing back at all. Such a refusal is now answered by
    /// sending the same request again without the setting.
    /// </summary>
    [TestFixture]
    public class OpenAIChatClientTemperatureTests
    {
        private const string TemperatureRefused =
            "{\"error\":{\"message\":\"Unsupported value: 'temperature' does not support 0.2 with this model. " +
            "Only the default (1) value is supported.\",\"type\":\"invalid_request_error\"," +
            "\"param\":\"temperature\",\"code\":\"unsupported_value\"}}";

        [Test]
        public void OpenAIRefusingTheTemperature_IsRecognised()
        {
            Assert.That(OpenAIChatClient.RejectsTemperature(400, TemperatureRefused), Is.True);
        }

        /// <summary>
        /// A service copying the shape loosely may not say which parameter it
        /// objects to, only mention it.
        /// </summary>
        [Test]
        public void ARefusalThatOnlyMentionsTheTemperature_IsRecognised()
        {
            Assert.That(OpenAIChatClient.RejectsTemperature(400,
                "{\"error\":{\"message\":\"temperature is not supported for this model\"}}"), Is.True);
        }

        /// <summary>
        /// Every other refusal is a refusal: sending the line again without the
        /// temperature would not change a missing model or a wrong key.
        /// </summary>
        [TestCase(404, "{\"error\":{\"message\":\"The model `gpt-6-luna` does not exist.\",\"param\":null,\"code\":\"model_not_found\"}}")]
        [TestCase(400, "{\"error\":{\"message\":\"Unsupported parameter: 'max_tokens'.\",\"param\":\"max_tokens\"}}")]
        [TestCase(401, "{\"error\":{\"message\":\"Incorrect API key provided.\"}}")]
        [TestCase(400, "")]
        public void AnyOtherRefusal_IsNotAboutTheTemperature(int status, string body)
        {
            Assert.That(OpenAIChatClient.RejectsTemperature(status, body), Is.False);
        }

        /// <summary>
        /// The status matters: the same words in a 500 are a broken server,
        /// to be retried as such, not a model with a view on its settings.
        /// </summary>
        [Test]
        public void TheSameWordsFromABrokenServer_AreNotARefusalOfTheTemperature()
        {
            Assert.That(OpenAIChatClient.RejectsTemperature(500, TemperatureRefused), Is.False);
        }

        [Test]
        public void ThePayload_CarriesTheTemperatureUntilTheModelRefusesIt()
        {
            var withIt = JObject.Parse(OpenAIChatClient.BuildPayload("gpt-4o-mini", "system", "Hello.", true));
            var without = JObject.Parse(OpenAIChatClient.BuildPayload("gpt-6-luna", "system", "Hello.", false));

            Assert.That(withIt["temperature"]?.Value<double>(), Is.EqualTo(0.2));
            Assert.That(without.ContainsKey("temperature"), Is.False);

            Assert.That(without["model"]?.ToString(), Is.EqualTo("gpt-6-luna"));
            Assert.That(without["messages"]?[1]?["content"]?.ToString(), Is.EqualTo("Hello."));
        }
    }
}
