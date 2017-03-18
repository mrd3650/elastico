﻿using System;
using System.Linq;
using System.Management.Automation;
using Xunit;
using Xunit.Abstractions;

namespace Elasticsearch.Powershell.Tests
{
    public class ElasticSearchTests : ElasticTest
    {
        private static readonly Domain.Person[] Data = CreateData();

        public ElasticSearchTests(ITestOutputHelper output)
            : base(output)
        {
            foreach(var person in Data)
            {
                var insertResponse = this.Client.Index(person);
                CheckResponse(insertResponse);
            }

            this.RefreshIndex();
        }

        [Fact]
        public void SearchAll()
        {
            var cmdlet = this.CreateCmdLet<ElasticSearch>();
            cmdlet.Index = new[] { this.DefaultIndex };
            var enumerator = cmdlet.Invoke().GetEnumerator();
            var found = 0;

            while(enumerator.MoveNext())
                found++;

            _output.WriteLine($"Found {found} records");
            Assert.Equal(Data.Length, found);
        }

        [Fact]
        public void SearchQuery()
        {
            //fields are camel case
            //https://www.elastic.co/guide/en/elasticsearch/client/net-api/2.x/field-inference.html
            var field = nameof(Domain.Person.Firstname).ToLower();

            var value = Data[0].Firstname;
            var count = Data.Count(p => p.Firstname.Equals(value));

            Assert.Equal(1, count);

            var cmdlet = this.CreateCmdLet<ElasticSearch>();
            cmdlet.Index = new[] { this.DefaultIndex };
            cmdlet.Query = $"{field}:{value}";
            var found = 0;

            foreach (PSObject record in cmdlet.Invoke())
            {
                _output.WriteLine(record.ToString());

                Assert.Equal(value, record.Properties[field].Value);
                found++;
            }

            _output.WriteLine($"Found {found} records");
            Assert.Equal(count, found);
        }

        [Fact]
        public void SearchFields()
        {
            //fields are camel case
            //https://www.elastic.co/guide/en/elasticsearch/client/net-api/2.x/field-inference.html
            var field = nameof(Domain.Person.Firstname).ToLower();

            var cmdlet = this.CreateCmdLet<ElasticSearch>();
            cmdlet.Index = new[] { this.DefaultIndex };
            cmdlet.Fields = new[] { field };
            var found = 0;

            foreach(PSObject record in cmdlet.Invoke())
            {
                _output.WriteLine(record.ToString());
                Assert.NotNull(record.Properties[field]);
                Assert.Equal(cmdlet.Fields.Length, record.Properties.Count());
                found++;
            }

            _output.WriteLine($"Found {found} records");
            Assert.Equal(Data.Length, found);
        }

        [Fact]
        public void ScrollApiTest()
        {
            var cmdlet = this.CreateCmdLet<ElasticSearch>();
            cmdlet.Index = new[] { this.DefaultIndex };
            cmdlet.Scroll = new SwitchParameter(true);
            var enumerator = cmdlet.Invoke().GetEnumerator();
            enumerator.MoveNext();
            var scrollId = (string)enumerator.Current;

            Assert.True(!String.IsNullOrWhiteSpace(scrollId));

            var cmdlet2 = this.CreateCmdLet<ElasticSearch>();
            cmdlet2.ScrollId = scrollId;

            var found = 0;
            foreach (PSObject record in cmdlet2.Invoke())
                found++;

            _output.WriteLine($"Found {found} records");
            Assert.Equal(Data.Length, found);
        }

        private static Domain.Person[] CreateData()
        {
            return new[]
            {
                new Domain.Person
                {
                    Id = 1,
                    Firstname = "Pinco",
                    Lastname = "Pallino"
                },

                new Domain.Person
                {
                    Id = 2,
                    Firstname = "John",
                    Lastname = "Doe"
                },

                new Domain.Person
                {
                    Id = 3,
                    Firstname = "Luigi",
                    Lastname = "Grilli"
                }
            };
        }
    }
}
