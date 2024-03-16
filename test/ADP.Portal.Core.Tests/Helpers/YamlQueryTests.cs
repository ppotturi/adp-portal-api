using ADP.Portal.Core.Helpers;
using Microsoft.VisualStudio.Services.TestManagement.TestPlanning.WebApi;
using NUnit.Framework;
using YamlDotNet.Serialization;

namespace ADP.Portal.Core.Tests.Helpers
{
    [TestFixture]
    public class YamlQueryTests
    {
        private YamlQuery? query;

        [Test]
        public void On_ListValue_Test()
        {
            // Arrange
            const string data = @"
            pods:
                - name:   pod1
                  desc:   Water Bucket
                  quantity:  4
                - name:   pod2
                  desc:   Air Ballons
                  quantity:  10
            ";

            using (var stream = new StringReader(data))
                query = new YamlQuery(new Deserializer().Deserialize(stream));

            // Act
            var actualValue = query
                            .On("pods")
                            //.Get("name")
                            .ToList<List<object>>();

            // Assert
            Assert.That(actualValue, Is.Not.Null);
            Assert.That(actualValue.Count, Is.EqualTo(1));
        }

        [Test]
        public void Get_StringValue_Test()
        {
            // Arrange
            const string data = @"
            pods:
                - name:   pod1
                  desc:   Water Bucket
                  quantity:  4
                - name:   pod2
                  desc:   Air Ballons
                  quantity:  10
            ";

            using (var stream = new StringReader(data))
                query = new YamlQuery(new Deserializer().Deserialize(stream));

            // Act
            var actualValue = query
                            .On("pods")
                            .Get("name")
                            .ToList<string>();

            // Assert
            Assert.That(actualValue, Is.Not.Null);
            Assert.That(actualValue.Count, Is.EqualTo(2));
        }

        [Test]
        public void Get_Nested_StringValue_Test()
        {
            // Arrange
            const string data = @"
            pods:
                - name:   pod1
                  desc:   Water Bucket
                  quantity:  4
                - name:   pod2
                  desc:   Air Ballons
                  quantity:  10
            service:
                name:   demo-service
                pods:
                    - name:   pod3
                      desc:   Fire Crackers
                      quantity:  15
            ";

            using (var stream = new StringReader(data))
                query = new YamlQuery(new Deserializer().Deserialize(stream));

            // Act
            var actualValue = query
                            .On("pods")
                            .Get("name")
                            .ToList<string>();

            // Assert
            Assert.That(actualValue, Is.Not.Null);
            Assert.That(actualValue.Count, Is.EqualTo(3));
        }

        [Test]
        public void Get_Nested_ListValue_Test()
        {
            // Arrange
            const string data = @"
            pods:
                - name:   pod1
                  desc:   Water Bucket
                  quantity:  4
                - name:   pod2
                  desc:   Air Ballons
                  quantity:  10
            service:
                name:   demo-service
                pods:
                    - name:   pod3
                      desc:   Fire Crackers
                      quantity:  15
                      type:
                        - name:     Sky Blast
                          quantity:     5
                        - name:     Sparklers
                          quantity:     10
            ";

            using (var stream = new StringReader(data))
                query = new YamlQuery(new Deserializer().Deserialize(stream));

            // Act
            var actualValue = query
                            .On("pods")
                            .Get("type")
                            .ToList<List<object>>();

            // Assert
            Assert.That(actualValue, Is.Not.Null);
            Assert.That(actualValue[0].Count, Is.EqualTo(2));
        }

        [Test]
        public void Remove_ListValue_Test()
        {
            // Arrange
            const string data = @"
            pods:
                - name:   pod1
                  desc:   Water Bucket
                  quantity:  4
                - name:   pod2
                  desc:   Air Ballons
                  quantity:  10
            service:
                name:   demo-service
                pods:
                    - name:   pod3
                      desc:   Fire Crackers
                      quantity:  15
            ";

            using (var stream = new StringReader(data))
                query = new YamlQuery(new Deserializer().Deserialize(stream));

            // Act
            var actualValue = query
                            .On("service")
                            .Remove("pods")
                            .ToList<Dictionary<object, object>>();

            // Assert
            Assert.That(actualValue, Is.Not.Null);
            Assert.That(actualValue[0].ContainsKey("name"), Is.True);
            Assert.That(actualValue[0].ContainsKey("pods"), Is.False);
        }

        [Test]
        public void Remove_Key_Not_Found_Test()
        {
            // Arrange
            const string data = @"
            pods:
                - name:   pod1
                  desc:   Water Bucket
                  quantity:  4
                - name:   pod2
                  desc:   Air Ballons
                  quantity:  10
            service:
                name:   demo-service
                pods:
                    - name:   pod3
                      desc:   Fire Crackers
                      quantity:  15
            ";

            using (var stream = new StringReader(data))
                query = new YamlQuery(new Deserializer().Deserialize(stream));

            // Act
            var actualValue = query
                            .On("service")
                            .Remove("rods")
                            .ToList<Dictionary<object, object>>();

            // Assert
            Assert.That(actualValue, Is.Not.Null);
            Assert.That(actualValue[0].ContainsKey("name"), Is.True);
            Assert.That(actualValue[0].ContainsKey("rods"), Is.False);
        }

        [Test]
        public void Remove_Empty_Key_Test()
        {
            // Arrange
            const string data = @"
            pods:
                - name:   pod1
                  desc:   Water Bucket
                  quantity:  4
                - name:   pod2
                  desc:   Air Ballons
                  quantity:  10
            ";

            using (var stream = new StringReader(data))
                query = new YamlQuery(new Deserializer().Deserialize(stream));

            // Act
            var actualValue = query
                            .On("pods")
                            .Remove(string.Empty)
                            .ToList<List<object>>();

            // Assert
            Assert.That(actualValue, Is.Not.Null);
            Assert.That(actualValue[0].Count, Is.EqualTo(2));
        }

        [Test]
        public void On_NullValue_Test()
        {
            // Arrange
            query = new YamlQuery(null);

            // Act
            var actualValue = query.On(string.Empty).ToList<object>();

            // Assert
            Assert.That(actualValue, Is.Not.Null);
            Assert.That(actualValue.Count, Is.EqualTo(0));
        }

        [Test]
        public void ToList_Error_Test()
        {
            // Arrange
            query = new YamlQuery(null);

            // Act

            // Assert
            Assert.Throws<InvalidOperationException>(() => query.ToList<string>());
        }

        [Test]
        public void Get_Error_Test()
        {
            // Arrange
            query = new YamlQuery(null);

            // Act

            // Assert
            Assert.Throws<InvalidOperationException>(() => query.Get(string.Empty));
        }

        [Test]
        public void Remove_Error_Test()
        {
            // Arrange
            query = new YamlQuery(null);

            // Act

            // Assert
            Assert.Throws<InvalidOperationException>(() => query.Remove(string.Empty));
        }
    }
}
