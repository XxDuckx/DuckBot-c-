using System.Collections.Generic;
using DuckBot.Core.Services;
using Xunit;

namespace DuckBot.Tests
{
    public class VariableEngineTests
    {
        [Fact]
        public void Substitute_ReplacesVariables()
        {
            var engine = new VariableEngine();
            var input = "Hello ${name}, number ${n}";
            var vars = new Dictionary<string, string?> { ["name"] = "Alice", ["n"] = "42" };
            var outp = engine.Substitute(input, vars);
            Assert.Equal("Hello Alice, number 42", outp);
        }

        [Fact]
        public void Substitute_MissingVar_EmptyString()
        {
            var engine = new VariableEngine();
            var input = "Missing ${nope}";
            var vars = new Dictionary<string, string?>();
            var outp = engine.Substitute(input, vars);
            Assert.Equal("Missing ", outp);
        }
    }
}