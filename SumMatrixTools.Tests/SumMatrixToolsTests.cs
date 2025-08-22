// File: SumMatrixToolsTests.cs
using System;
using System.Linq;
using System.Text.Json;
using Sum.Mcp.Server.Capabilities;
using Xunit;

namespace SumMatrixTools.Tests
{
    public class SumMatrixToolsTests
    {
        private static JsonElement ToJsonElement(object o)
        {
            var json = JsonSerializer.Serialize(o);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }

        [Fact]
        public void Hello_Default_ReturnsHelloSumMatrix()
        {
            var result = SumMatrixTools.Hello();
            var root = ToJsonElement(result);
            Assert.Equal("Hello Sum Matrix!", root.GetProperty("message").GetString());
        }

        [Fact]
        public void Hello_Name_ReturnsPersonalizedGreeting()
        {
            var result = SumMatrixTools.Hello("Developer");
            var root = ToJsonElement(result);
            Assert.Equal("Hello Developer!", root.GetProperty("message").GetString());
        }

        [Fact]
        public void MathAdd_Works()
        {
            var result = SumMatrixTools.MathAdd(3, 2);
            var root = ToJsonElement(result);
            Assert.Equal(5, root.GetProperty("result").GetDouble());
        }

        [Fact]
        public void MathSum_Works()
        {
            var result = SumMatrixTools.MathSum(new double[] { 1, 2, 3 });
            var root = ToJsonElement(result);
            Assert.Equal(3, root.GetProperty("count").GetInt32());
            Assert.Equal(6, root.GetProperty("sum").GetDouble());
        }

        [Fact]
        public void MathAvg_Empty_ReturnsNullAverage()
        {
            var result = SumMatrixTools.MathAvg(Array.Empty<double>());
            var root = ToJsonElement(result);
            Assert.Equal(0, root.GetProperty("count").GetInt32());
            Assert.True(root.GetProperty("average").ValueKind == JsonValueKind.Null);
        }

        [Fact]
        public void Slugify_Basic()
        {
            var result = SumMatrixTools.Slugify("Hello, World!");
            var root = ToJsonElement(result);
            Assert.Equal("hello-world", root.GetProperty("slug").GetString());
        }

        [Fact]
        public void ExtractEmails_FindsOne()
        {
            var text = "Contact me at test@example.com for details.";
            var result = SumMatrixTools.ExtractEmails(text);
            var root = ToJsonElement(result);
            Assert.Equal(1, root.GetProperty("count").GetInt32());
            Assert.Contains("test@example.com", root.GetProperty("emails").EnumerateArray().Select(e => e.GetString()));
        }

        [Fact]
        public void JsonValidate_Valid()
        {
            var json = "{\"name\": \"Nitay\", \"ok\": true}";
            var result = SumMatrixTools.JsonValidate(json);
            var root = ToJsonElement(result);
            Assert.True(root.GetProperty("valid").GetBoolean());
            Assert.True(root.GetProperty("pretty").GetString()?.Contains("") ?? false);
        }

        [Fact]
        public void Time_Parse_UtcIso()
        {
            var result = SumMatrixTools.ParseTime("2025-01-02T03:04:05Z");
            var root = ToJsonElement(result);
            var iso = root.GetProperty("iso").GetString();
            Assert.StartsWith("2025-01-02T03:04:05", iso);
            Assert.EndsWith("Z", iso);
        }
    }
}
