using NUnit.Framework;

using Translation.Providers.AI;

namespace Translation.Tests.Providers
{
    /// <summary>
    /// What the player is told when a chat-completions service says no.
    ///
    /// Reported 2026-10-06: a player with their own OpenAI key named the model
    /// they wanted, and every model from gpt-4 to gpt-6 came back as the same
    /// "Translation failed: OpenAI". The service had said why each time; it
    /// went to the log and nowhere else.
    /// </summary>
    [TestFixture]
    public class OpenAIChatClientRefusalTests
    {
        [Test]
        public void OpenAIsOwnExplanation_IsWhatThePlayerReads()
        {
            const string body =
                "{\"error\":{\"message\":\"The model `gpt-6-luna ` does not exist or you do not have access to it.\"," +
                "\"type\":\"invalid_request_error\",\"param\":null,\"code\":\"model_not_found\"}}";

            Assert.That(OpenAIChatClient.DescribeRefusal(404, body),
                Is.EqualTo("HTTP 404: The model `gpt-6-luna ` does not exist or you do not have access to it."));
        }

        [Test]
        public void ASettingTheModelDoesNotTake_IsNamed()
        {
            const string body =
                "{\"error\":{\"message\":\"Unsupported value: 'temperature' does not support 0.2 with this model. " +
                "Only the default (1) value is supported.\",\"type\":\"invalid_request_error\"," +
                "\"param\":\"temperature\",\"code\":\"unsupported_value\"}}";

            Assert.That(OpenAIChatClient.ReadServiceError(body), Does.StartWith("Unsupported value: 'temperature'"));
        }

        /// <summary>Ollama puts the explanation straight into "error".</summary>
        [Test]
        public void AnErrorThatIsJustAString_IsReadToo()
        {
            Assert.That(OpenAIChatClient.ReadServiceError("{\"error\":\"model 'qwen9' not found\"}"),
                Is.EqualTo("model 'qwen9' not found"));
        }

        [Test]
        public void ABodyThatIsNotJson_IsShownAsItCame()
        {
            Assert.That(OpenAIChatClient.ReadServiceError("Bad Gateway\r\n"), Is.EqualTo("Bad Gateway"));
        }

        [Test]
        public void ALongBody_IsCutShort()
        {
            var reason = OpenAIChatClient.ReadServiceError("<html>" + new string('x', 1000) + "</html>");

            Assert.That(reason.Length, Is.LessThanOrEqualTo(303));
            Assert.That(reason, Does.EndWith("..."));
        }

        /// <summary>
        /// JSON, but not an object - nothing to look an "error" up in. Shown
        /// as it came rather than tripping over the lookup.
        /// </summary>
        [TestCase("[1,2,3]")]
        [TestCase("\"overloaded\"")]
        public void JsonThatIsNotAnObject_IsShownAsItCame(string body)
        {
            Assert.That(OpenAIChatClient.ReadServiceError(body), Is.EqualTo(body));
            Assert.That(OpenAIChatClient.RejectsTemperature(400, body), Is.False);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void NothingSaid_LeavesTheStatusAlone(string body)
        {
            Assert.That(OpenAIChatClient.DescribeRefusal(403, body), Is.EqualTo("HTTP 403"));
        }
    }
}
