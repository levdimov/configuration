﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Commons.Conversions;
using Vostok.Commons.Testing;
using Vostok.Configuration.Sources;

namespace Vostok.Configuration.Tests.Sources
{
    [TestFixture]
    public class ScopedSource_Tests
    {
        private const string TestFileName = "test_ScopedSource.json";

        [TearDown]
        public void Cleanup()
        {
            File.Delete(TestFileName);
        }

        private static void CreateTextFile(string text)
        {
            using (var file = new StreamWriter(TestFileName, false))
                file.WriteLine(text);
        }

        [Test]
        public void Should_return_full_tree()
        {
            CreateTextFile("{ \"value\": 1 }");
            var jfs = new JsonFileSource(TestFileName);

            using (var ss = new ScopedSource(jfs))
                ss.Get().Should().BeEquivalentTo(new RawSettings(
                    new Dictionary<string, RawSettings>
                    {
                        { "value", new RawSettings("1") },
                    }));
        }

        [Test]
        public void Should_scope_by_dictionaries_keys()
        {
            CreateTextFile("{ \"value 1\": { \"value 2\": { \"value 3\": 1 } } }");
            var jfs = new JsonFileSource(TestFileName);

            using (var ss = new ScopedSource(jfs, "value 1", "value 2"))
                ss.Get().Should().BeEquivalentTo(new RawSettings(
                    new Dictionary<string, RawSettings>
                    {
                        { "value 3", new RawSettings("1") },
                    }));

            using (var ss = new ScopedSource(jfs, "value 1", "value 2", "value 3"))
                ss.Get().Should().BeEquivalentTo(new RawSettings("1"));
        }

        [Test]
        public void Should_scope_by_list_indexes()
        {
            CreateTextFile("{ \"value\": [[1,2], [3,4,5]] }");
            var jfs = new JsonFileSource(TestFileName);

            using (var ss = new ScopedSource(jfs, "value", "[0]"))
                ss.Get()
                .Should().BeEquivalentTo(new RawSettings(
                    new List<RawSettings>
                    {
                        new RawSettings("1"),
                        new RawSettings("2"),
                    }));

            using (var ss = new ScopedSource(jfs, "value", "[1]", "[2]"))
                ss.Get().Should().BeEquivalentTo(new RawSettings("5"));
        }

        [Test]
        public void Should_return_null()
        {
            CreateTextFile("{ \"value\": { \"list\": [1,2] } }");
            var jfs = new JsonFileSource(TestFileName);

            using (var ss = new ScopedSource(jfs, "unknown value"))
                ss.Get().Should().BeNull();
            using (var ss = new ScopedSource(jfs, "value", "[0]"))
                ss.Get().Should().BeNull();
            using (var ss = new ScopedSource(jfs, "value", "list", "[]"))
                ss.Get().Should().BeNull();
            using (var ss = new ScopedSource(jfs, "value", "list", "[not_a_number]"))
                ss.Get().Should().BeNull();
            using (var ss = new ScopedSource(jfs, "value", "list", "[100]"))
                ss.Get().Should().BeNull();
        }

        [Test]
        public void Should_observe_file()
        {
            new Action(() => ShouldObserveFileTest().Should().BeEquivalentTo(
                new List<RawSettings>
                {
                    new RawSettings("2"),
                    new RawSettings("4"),
                }
            )).ShouldPassIn(1.Seconds());
        }

        private List<RawSettings> ShouldObserveFileTest()
        {
            CreateTextFile("{ \"value\": { \"list\": [1,2] } }");
            var jfs = new JsonFileSource(TestFileName, 100.Milliseconds());
            var rsList = new List<RawSettings>();

            using (var ss = new ScopedSource(jfs, "value", "list", "[1]"))
            {
                var sub = ss.Observe().Subscribe(settings => rsList.Add(settings));

                Thread.Sleep(200.Milliseconds());
                CreateTextFile("{ \"value\": { \"list\": [3,4,5] } }");
                Thread.Sleep(200.Milliseconds());

                sub.Dispose();
            }
            return rsList;
        }
    }
}