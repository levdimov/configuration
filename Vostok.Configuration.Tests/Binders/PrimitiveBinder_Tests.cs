﻿using System;
using System.Collections.Specialized;
using System.Net;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Commons;
using Vostok.Configuration.Binders;

namespace Vostok.Configuration.Tests.Binders
{
    public class PrimitiveBinder_Tests: Binders_Test
    {
        [TestCase("FaLsE", false, TestName = "BoolValue")]
        [TestCase("255", (byte)255, TestName = "ByteValue")]
        [TestCase("q", 'q', TestName = "CharValue")]
        [TestCase("123.456", 123.456d, TestName = "DoubleValue")]
        [TestCase("123,456", 123.456f, TestName = "FloatValue")]
        [TestCase("12345", 12345, TestName = "IntValue")]
        [TestCase("123456787654321", 123456787654321L, TestName = "LongValue")]
        [TestCase("-1", (sbyte)-1, TestName = "SbyteValue")]
        [TestCase("100", (short)100, TestName = "ShortValue")]
        [TestCase("1234567", (uint)1234567, TestName = "UintValue")]
        [TestCase("123456787654321", (ulong)123456787654321L, TestName = "UlongValue")]
        [TestCase("200", (ushort)200, TestName = "UshortValue")]
        public void Should_bind_to_Primitive<T>(string value, T res)
        {
            var settings = new RawSettings(value);
            var binder = Container.GetInstance<ISettingsBinder<T>>();
            var result = binder.Bind(settings);
            result.Should().Be(res);
        }

        [Test]
        public void Should_bind_to_Decimal()
        {
            var settings = new RawSettings("123,456");
            var binder = Container.GetInstance<ISettingsBinder<decimal>>();
            var result = binder.Bind(settings);
            result.Should().Be(123.456m);
        }

        [Test]
        public void Should_bind_to_Primitive_from_single_dictionary_value()
        {
            var settings = new RawSettings(new OrderedDictionary
            {
                { "key", new RawSettings("123") }
            });
            var binder = Container.GetInstance<ISettingsBinder<int>>();
            var result = binder.Bind(settings);
            result.Should().Be(123);
        }

        [Test]
        public void Should_bind_to_TimeSpan()
        {
            var settings = new RawSettings("1 second");
            var binder = Container.GetInstance<ISettingsBinder<TimeSpan>>();
            binder.Bind(settings).Should().Be(new TimeSpan(0, 0, 0, 1, 0));
        }

        [Test]
        public void Should_bind_to_IPAddress()
        {
            var settings = new RawSettings("192.168.1.10");
            var binder = Container.GetInstance<ISettingsBinder<IPAddress>>();
            binder.Bind(settings).Should().Be(
                new IPAddress(new byte[] { 192, 168, 1, 10 }));

            settings = new RawSettings("2001:0db8:11a3:09d7:1f34:8a2e:07a0:765d");
            binder = Container.GetInstance<ISettingsBinder<IPAddress>>();
            binder.Bind(settings).Should().Be(
                new IPAddress(new byte[] { 32, 1, 13, 184, 17, 163, 9, 215, 31, 52, 138, 46, 7, 160, 118, 93 }));
        }

        [Test]
        public void Should_bind_to_IPEndPoint()
        {
            var settings = new RawSettings("192.168.1.10:80");
            var binder = Container.GetInstance<ISettingsBinder<IPEndPoint>>();
            binder.Bind(settings).Should().Be(
                new IPEndPoint(new IPAddress(new byte[] { 192, 168, 1, 10 }), 80));
        }

        [Test]
        public void Should_bind_to_DataRate()
        {
            var settings = new RawSettings("10/sec");
            var binder = Container.GetInstance<ISettingsBinder<DataRate>>();
            binder.Bind(settings).Should().Be(
                DataRate.FromBytesPerSecond(10));
        }

        [Test]
        public void Should_bind_to_DataSize()
        {
            var settings = new RawSettings("10 bytes");
            var binder = Container.GetInstance<ISettingsBinder<DataSize>>();
            binder.Bind(settings).Should().Be(DataSize.FromBytes(10));
        }

        [Test]
        public void Should_bind_to_Guid()
        {
            var guid = "936DA01F-9ABD-4d9d-80C7-02AF85C822A8";
            var settings = new RawSettings(guid);
            var binder = Container.GetInstance<ISettingsBinder<Guid>>();
            binder.Bind(settings).Should().Be(new Guid(guid));
        }

        [Test]
        public void Should_bind_to_Uri()
        {
            var settings = new RawSettings("http://example.com");
            var binder = Container.GetInstance<ISettingsBinder<Uri>>();
            binder.Bind(settings).Should().Be(new Uri("http://example.com", UriKind.Absolute));
        }

        [Test]
        public void Should_bind_to_String()
        {
            var settings = new RawSettings("somestring");
            var binder = Container.GetInstance<ISettingsBinder<string>>();
            binder.Bind(settings).Should().Be("somestring");
        }
    }
}