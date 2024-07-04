﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FreeTrainSimulator.Common.Info;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Orts.Common.Info
{
    [TestClass]
    public class RuntimeInfoTests
    {
        [TestMethod]
        public void ApplicationNameTest()
        {
            string expected = RuntimeInfo.ApplicationName;
            Assert.AreEqual("Free Train Simulator Unit Tests", expected);
        }

        [TestMethod]
        public void ProductNameTest()
        {
            string expected = RuntimeInfo.ProductName;
            Assert.AreEqual("Free Train Simulator", expected);
        }
    }
}
