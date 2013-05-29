using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Piranha.Tests.TestCases {
    class TestBase {

        public TestBase(bool zzz) {
        }

        public TestBase(string arg, TestBase xxx) {
        }

        public static string Method1(int arg) {
            return "";
        }

        public TestBase This {
            get { return this; }
        }
    }

    class TestDerived : TestBase {

        public TestDerived(bool arg1, int arg2)
            : base(TestBase.Method1(arg2), new TestBase(true).This) {
        }
    }

    class TestBase2 {
        public TestBase2(bool arg) { }
        public TestBase2(int arg) { }
        public TestBase2(long arg) { }
        public TestBase2(TimeSpan arg) { }
        public TestBase2(string arg) { }
        public TestBase2(ref long arg) { }
        public TestBase2(out TimeSpan arg) { arg = default(TimeSpan); }
    }

    class TestDerived2 : TestBase2 {
        public TestDerived2(bool arg) : base(default(bool)) { }
        public TestDerived2(int arg) : base(default(int)) { }
        public TestDerived2(long arg) : base(default(long)) { }
        public TestDerived2(TimeSpan arg) : base(default(TimeSpan)) { }
        public TestDerived2(string arg) : base(default(string)) { }
        public TestDerived2(ref long arg) : base(ref arg) { }
        public TestDerived2(out TimeSpan arg) : base(out arg) { }

        public void TestMethod(ref long arg) { }
        public void TestMethod(out TimeSpan arg) { arg = default(TimeSpan); }
    }

    class TestBase3<T> {
        public TestBase3(T arg) { }
    }

    class TestDerived3a<T> : TestBase3<T> {
        public TestDerived3a(T arg) : base(default(T)) { }
    }

    class TestDerived3b : TestBase3<int> {
        public TestDerived3b() : base(default(int)) { }
    }

    class TestDerived3c : TestBase3<string> {
        public TestDerived3c() : base(default(string)) { }
    }
}
