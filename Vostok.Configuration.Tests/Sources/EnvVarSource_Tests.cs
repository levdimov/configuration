﻿using System;
using System.Collections.Generic;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Commons.Conversions;
using Vostok.Commons.Testing;
using Vostok.Configuration.Sources;

namespace Vostok.Configuration.Tests.Sources
{
    [TestFixture]
    public class EnvVarSource_Tests
    {
        [Test]
        public void Should_return_null_if_file_not_exists()
        {
            using (var evs = new EnvVarSource())
            {
                var res = evs.Get();
                res.ChildrenByKey.Keys.Should().Contain("Path").And.Contain("APPDATA");
            }
        }

        [Test]
        public void Should_observe_variables()
        {
            new Action(() => ShouldObserveVariablesTest().Should().Be(1)).ShouldPassIn(1.Seconds());
        }
        private int ShouldObserveVariablesTest()
        {
            const string testVar = "test_key";
            const string testValue = "test_value";
            var val = 0;
            using (var evs = new EnvVarSource(100.Milliseconds()))
            {
                var sub = evs.Observe().Subscribe(settings =>
                {
                    if (settings.ChildrenByKey.ContainsKey(testVar))
                        val++;
                });

                Thread.Sleep(200.Milliseconds());
                Environment.SetEnvironmentVariable(testVar, testValue);
                Thread.Sleep(200.Milliseconds());

                sub.Dispose();
            }
            return val;
        }
    }
}