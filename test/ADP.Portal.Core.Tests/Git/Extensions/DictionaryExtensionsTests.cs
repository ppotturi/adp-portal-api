using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Extensions;
using NSubstitute;
using NUnit.Framework;

namespace ADP.Portal.Core.Tests.Git.Extensions
{

    [TestFixture]
    public class DictionaryExtensionsTests
    {
        private Dictionary<string, Dictionary<object, object>> instanceDictionary;
        private List<object> instanceList;
        private readonly FluxConfig config;


        [SetUp]
        public void Setup()
        {
            instanceDictionary = new Dictionary<string, Dictionary<object, object>>();
            instanceList = new List<object>();
        }

        public DictionaryExtensionsTests()
        {
            instanceDictionary = new Dictionary<string, Dictionary<object, object>>();
            instanceList = new List<object>();
            config = new FluxConfig() { Key = "key1", Value = "value1" };
        }

        [Test]
        public void ReplaceToken_DictionaryInstance_DictionaryValue_Test()
        {
            // Arrange
            var innerDictionaryMock = Substitute.For<Dictionary<object, object>>();
            instanceDictionary.Add("dictionary_key1", new Dictionary<object, object> { { "child_key1", innerDictionaryMock } });

            // Act
            instanceDictionary.ReplaceToken(config);

            // Assert
            innerDictionaryMock.Received().ReplaceToken(config);
        }

        [Test]
        public void ReplaceToken_DictionaryInstance_ListValue_Test()
        {
            // Arrange
            var listMock = Substitute.For<List<object>>();
            var dictionaryObject = new Dictionary<object, object>
            {
                { "List_key1", listMock }
            };
            instanceDictionary.Add("dictionary_key1", dictionaryObject);

            // Act
            instanceDictionary.ReplaceToken(config);

            // Assert
            listMock.Received().ReplaceToken(config);
        }

        [Test]
        public void ReplaceToken_DictionaryInstance_StringValue_Test()
        {
            // Arrange
            var originalValue = "__key1__";
            var expectedValue = "value1";

            var dictionaryObject = new Dictionary<object, object>
            {
                { "key", originalValue }
            };

            instanceDictionary.Add("dictionary_key1", dictionaryObject);

            // Act
            instanceDictionary.ReplaceToken(config);

            // Assert
            Assert.That(dictionaryObject["key"], Is.EqualTo(expectedValue));
        }

        [Test]
        public void ReplaceToken_DictionaryInstance_EmptyStringValue_Test()
        {
            // Arrange
            var originalValue = "";
            var expectedValue = "";

            var dictionaryObject = new Dictionary<object, object>
            {
                { "key", originalValue }
            };

            instanceDictionary.Add("dictionary_key1", dictionaryObject);

            // Act
            instanceDictionary.ReplaceToken(config);

            // Assert
            Assert.That(dictionaryObject["key"], Is.EqualTo(expectedValue));
        }

        [Test]
        public void ReplaceToken_DictionaryInstance_ListValue_ListValue_Test()
        {
            // Arrange

            var expectedReplaceValue = "list_object_value1";
            var listMock = Substitute.For<Dictionary<object, object>>();
            var listObject = new List<object>
            {
                new List<object>() { "list_object___key1__" }
            };

            listMock.Add("list1", new List<object>() { listObject });
            instanceDictionary.Add("dictionary_key1", listMock);

            // Act
            instanceDictionary.ReplaceToken(config);

            // Assert
            listMock.Received().ReplaceToken(config);
            var list1object = ((List<object>?)((List<object>)listMock["list1"]).FirstOrDefault())?.FirstOrDefault();
            if (list1object != null)
            {
                Assert.That(((List<object>)list1object)[0], Is.EqualTo(expectedReplaceValue));
            }
        }

        [Test]
        public void ReplaceToken_ListInstance_DictionaryValue_Test()
        {
            // Arrange
            var expectedReplaceValue = "dictionary_object_value1";
            var dictionaryMock = Substitute.For<Dictionary<object, object>>();
            dictionaryMock.Add("dictionary_key1", "dictionary_object___key1__");
            instanceList.Add(dictionaryMock);

            // Act
            instanceList.ReplaceToken(config);

            // Assert
            dictionaryMock.Received().ReplaceToken(config);
            Assert.That(dictionaryMock["dictionary_key1"], Is.EqualTo(expectedReplaceValue));
        }

        [Test]
        public void ReplaceToken_ListInstance_ListValue_Test()
        {
            // Arrange
            var expectedReplaceValue = "list_object_value1";
            var listMock = Substitute.For<List<object>>();
            listMock.Add("list_object___key1__");

            var listObect = new List<object>
            {
                listMock
            };

            instanceList.Add(listObect);

            // Act
            instanceList.ReplaceToken(config);

            // Assert
            listMock.Received().ReplaceToken(config);
            Assert.That(listMock[0], Is.EqualTo(expectedReplaceValue));

        }

        [Test]
        public void ReplaceToken_ListInstance_Value_Test()
        {
            // Arrange
            var expectedReplaceValue = "list_object_value1";
            var listMock = Substitute.For<List<object>>();
            listMock.Add("list_object___key1__");
            instanceList.Add(listMock);

            // Act
            instanceList.ReplaceToken(config);

            // Assert
            listMock.Received().ReplaceToken(config);
            Assert.That(listMock[0], Is.EqualTo(expectedReplaceValue));
        }

        [Test]
        public void ReplaceToken_ListInstance_EmptyValue_Test()
        {
            // Arrange
            string expectedReplaceValue = null;
            var listMock = Substitute.For<List<object>>();
            listMock.Add(null);
            instanceList.Add(listMock);

            // Act
            instanceList.ReplaceToken(config);

            // Assert
            listMock.Received().ReplaceToken(config);
            Assert.That(listMock[0], Is.EqualTo(expectedReplaceValue));
        }
    }
}
