using System;
using System.Collections.Generic;
using GenesisUI.Data;
using Xunit;

namespace GenesisUI.Core.Tests
{
    public class StrictJsonTests
    {
        [Fact]
        public void Reads_objects_arrays_and_scalars()
        {
            var o = (Dictionary<string, object>)StrictJson.Parse("{\"a\":1,\"b\":[true,false,null],\"c\":\"x\\\"y\\u00e7\",\"d\":-2.5e1}");

            Assert.Equal(1.0, o["a"]);
            Assert.Equal(new object[] { true, false, null }, (List<object>)o["b"]);
            Assert.Equal("x\"yç", o["c"]);
            Assert.Equal(-25.0, o["d"]);
        }

        [Theory]
        [InlineData("")]
        [InlineData("{")]
        [InlineData("{\"a\":1,}")]
        [InlineData("[1 2]")]
        [InlineData("{\"a\":1}{")]
        [InlineData("{\"a\":1,\"a\":2}")]
        [InlineData("{a:1}")]
        [InlineData("'x'")]
        [InlineData("\"tab\there\"")]
        [InlineData("1.2.3")]
        [InlineData("tru")]
        public void Malformed_input_throws(string json)
        {
            Assert.Throws<FormatException>(() => StrictJson.Parse(json));
        }

        [Fact]
        public void Depth_and_size_are_bounded()
        {
            string deep = new string('[', StrictJson.MaxDepth + 2) + new string(']', StrictJson.MaxDepth + 2);
            Assert.Throws<FormatException>(() => StrictJson.Parse(deep));
            Assert.Throws<FormatException>(() => StrictJson.Parse(new string(' ', StrictJson.MaxLength + 1)));
        }

        [Fact]
        public void A_bom_is_tolerated()
        {
            Assert.Equal(3.0, StrictJson.Parse("﻿ 3 "));
        }
    }
}
