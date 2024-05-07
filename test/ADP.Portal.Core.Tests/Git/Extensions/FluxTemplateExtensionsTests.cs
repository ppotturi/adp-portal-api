using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Extensions;
using NSubstitute;
using NUnit.Framework;

namespace ADP.Portal.Core.Tests.Git.Extensions
{

    [TestFixture]
    public class FluxTemplateExtensionsTests
    {
        private Dictionary<string, FluxTemplateFile> items;
        private List<object> instanceList;
        private readonly Dictionary<object, object> copyInstanceDictionary;
        private readonly FluxConfig config;


        [SetUp]
        public void Setup()
        {
            items = [];
            instanceList = [];
        }

        public FluxTemplateExtensionsTests()
        {
            items = [];
            instanceList = [];
            copyInstanceDictionary = [];
            config = new FluxConfig() { Key = "key1", Value = "value1" };
        }

        [Test]
        public void ReplaceToken_DictionaryInstance_DictionaryValue_Test()
        {
            // Arrange
            var innerDictionaryMock = Substitute.For<Dictionary<object, object>>();
            items.Add("dictionary_key1", new FluxTemplateFile(new Dictionary<object, object> { { "child_key1", innerDictionaryMock } }));

            // Act
            items.ReplaceToken(config);

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
            items.Add("dictionary_key1", new FluxTemplateFile(dictionaryObject));

            // Act
            items.ReplaceToken(config);

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

            items.Add("dictionary_key1", new FluxTemplateFile(dictionaryObject));

            // Act
            items.ReplaceToken(config);

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

            items.Add("dictionary_key1", new FluxTemplateFile( dictionaryObject));

            // Act
            items.ReplaceToken(config);

            // Assert
            Assert.That(dictionaryObject["key"], Is.EqualTo(expectedValue));
        }

        [Test]
        public void ReplaceToken_DictionaryInstance_ListValue_ListValue_Test()
        {
            // Arrange

            var expectedReplaceValue = "list_object_value1";
            var listMock = new FluxTemplateFile([]);
            var listObject = new List<object>
            {
                new List<object>() { "list_object___key1__" }
            };

            listMock.Content.Add("list1", new List<object>() { listObject });
            items.Add("dictionary_key1", listMock);

            // Act
            items.ReplaceToken(config);

            // Assert
            var list1object = ((List<object>?)((List<object>)listMock.Content["list1"]).FirstOrDefault())?.FirstOrDefault();
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
            Assert.That(listMock[0], Is.EqualTo(expectedReplaceValue));
        }

        [Test]
        public void ReplaceToken_ListInstance_EmptyValue_Test()
        {
            // Arrange
            string? expectedReplaceValue = null;
            var listMock = Substitute.For<List<object?>>();
            listMock.Add(expectedReplaceValue);
            instanceList.Add(listMock);

            // Act
            instanceList.ReplaceToken(config);

            // Assert
            Assert.That(listMock[0], Is.EqualTo(expectedReplaceValue));
        }

        [Test]
        public void DeepCopy_DictionaryInstance_DictionaryValue_Test()
        {
            // Arrange
            copyInstanceDictionary.Add("key1", Substitute.For<Dictionary<object, object>>());

            // Act
            var actualVal = copyInstanceDictionary.DeepCopy();

            // Assert
            Assert.That(actualVal.ContainsKey("key1"), Is.EqualTo(true));
        }

        [Test]
        public void DeepCopy_DictionaryInstance_EmptyValue_Test()
        {
            // Arrange
            copyInstanceDictionary.Clear();

            // Act
            var actual = copyInstanceDictionary.DeepCopy();

            // Assert
            Assert.That(actual.Count, Is.EqualTo(0));
        }
    }
}
