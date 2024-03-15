using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Extensions;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADP.Portal.Core.Tests.Git.Extensions
{
    
    [TestFixture]
    public class DictionaryExtensionsTests
    {
        private readonly Dictionary<string, Dictionary<object, object>> instance;
        private readonly FluxConfig config;
      

        public DictionaryExtensionsTests()
        {
            instance = new Dictionary<string, Dictionary<object, object>>();
            config = new FluxConfig() { Key = "key1", Value = "value1" };
        }

        [Test]
        public void ReplaceToken_DictionaryType_DictionaryValue_Test()
        {
            // Arrange
            var innerDictionary = Substitute.For<Dictionary<object, object>>();
            instance.Add("key1", innerDictionary);

            // Act
            instance.ReplaceToken(config);

            // Assert
            innerDictionary.Received().ReplaceToken(config);
        }

        [Test]
        public void ReplaceToken_DictionaryType_ListValue_Test()
        {
            // Arrange
            var listValue = Substitute.For<Dictionary<object,object>>();
            listValue.Add("ley1", new List<object>());
            instance.Add("key2", listValue);

            // Act
            instance.ReplaceToken(config);

            // Assert
            listValue.Received().ReplaceToken(config);
        }

        [Test]
        public void ReplaceToken_DictionaryType_ListValue_DictionaryType_Test()
        {
            // Arrange
            var listValue = Substitute.For<Dictionary<object, object>>();
            var listDictonary = new Dictionary<object, object>
            {
                { "list_dictonary1", "list_dictonary_value" }
            };
            listValue.Add("list1", new List<object>() { listDictonary } );
            instance.Add("key2", listValue);

            // Act
            instance.ReplaceToken(config);

            // Assert
            listValue.Received().ReplaceToken(config);
        }

        [Test]
        public void ReplaceToken_DictionaryType_StringValue_Test()
        {
            // Arrange
            var originalValue = "__key1__";
            var expectedValue = "value1";

            var template = new Dictionary<object, object>
            {
                { "key", originalValue }
            };

            instance.Add("key3", template);

            // Act
            instance.ReplaceToken(config);

            // Assert
            Assert.That(expectedValue, Is.EqualTo(template["key"]));
        }

    }
}
