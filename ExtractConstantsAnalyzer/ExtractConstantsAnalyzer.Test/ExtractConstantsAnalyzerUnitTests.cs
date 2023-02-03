﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using VerifyCS = ExtractConstantsAnalyzer.Test.CSharpCodeFixVerifier<
    ExtractConstantsAnalyzer.ExtractConstantsAnalyzerAnalyzer,
    ExtractConstantsAnalyzer.ExtractConstantsAnalyzerCodeFixProvider>;

namespace ExtractConstantsAnalyzer.Test;

[TestClass]
public class ExtractConstantsAnalyzerUnitTest
{
    //No diagnostics expected to show up
    [TestMethod]
    public async Task EmptyText()
    {
        var test = @"";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [TestMethod]
    public async Task AddNewConstants()
    {
        var test = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
    class MyClass
    {
        private const string UnrelatedConstant = ""Unrelated"";

        public void MyMethod(string myarg1, int myarg2)
        {
            if(myarg1 == {|#0:""MyConstant""|})
            {
            }
            if(myarg2 == {|#1:123|})
            {
            }
        }
    }
}";

        var fixtest = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
    class MyClass
    {
        private const int MyClass123 = 123;
        private const string MyClassMyConstant = ""MyConstant"";
        private const string UnrelatedConstant = ""Unrelated"";

        public void MyMethod(string myarg1, int myarg2)
        {
            if(myarg1 == MyClassMyConstant)
            {
            }
            if(myarg2 == MyClass123)
            {
            }
        }
    }
}";

        var expected0 = VerifyCS.Diagnostic("ExtractConstantsAnalyzer")
            .WithLocation(0).WithArguments("MyConstant")
            ;
        var expected1 = VerifyCS.Diagnostic("ExtractConstantsAnalyzer")
            .WithLocation(1).WithArguments("123")
            ;
        await VerifyCS.VerifyCodeFixAsync(test, 2, new[] { expected0, expected1 }, fixtest);
    }

    [TestMethod]
    public async Task UseExistingConstants()
    {
        var test = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
    class MyClass
    {
        private const string MyClassMyConstant = ""MyConstant"";
        private const int MyClass123 = 123;
        private const string UnrelatedConstant = ""Unrelated"";

        public void MyMethod(string myarg1, int myarg2)
        {
            if(myarg1 == {|#0:""MyConstant""|})
            {
            }
            if(myarg2 == {|#1:123|})
            {
            }
        }
    }
}";

        var fixtest = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
    class MyClass
    {
        private const string MyClassMyConstant = ""MyConstant"";
        private const int MyClass123 = 123;
        private const string UnrelatedConstant = ""Unrelated"";

        public void MyMethod(string myarg1, int myarg2)
        {
            if(myarg1 == MyClassMyConstant)
            {
            }
            if(myarg2 == MyClass123)
            {
            }
        }
    }
}";

        var expected0 = VerifyCS.Diagnostic("ExtractConstantsAnalyzer")
            .WithLocation(0).WithArguments("MyConstant")
            ;
        var expected1 = VerifyCS.Diagnostic("ExtractConstantsAnalyzer")
            .WithLocation(1).WithArguments("123")
            ;
        await VerifyCS.VerifyCodeFixAsync(test, 1, new[] { expected0, expected1 }, fixtest);
    }

    [TestMethod]
    public async Task UseExistingContantsWithDifferentNames()
    {
        var test = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
    class MyClass
    {
        private const string MyFirstConstant = ""MyConstant"";
        private const int MySecondConstant = 123;
        private const string UnrelatedConstant = ""Unrelated"";

        public void MyMethod(string myarg1, int myarg2)
        {
            if(myarg1 == {|#0:""MyConstant""|})
            {
            }
            if(myarg2 == {|#1:123|})
            {
            }
        }
    }
}";

        var fixtest = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
    class MyClass
    {
        private const string MyFirstConstant = ""MyConstant"";
        private const int MySecondConstant = 123;
        private const string UnrelatedConstant = ""Unrelated"";

        public void MyMethod(string myarg1, int myarg2)
        {
            if(myarg1 == MyFirstConstant)
            {
            }
            if(myarg2 == MySecondConstant)
            {
            }
        }
    }
}";

        var expected0 = VerifyCS.Diagnostic("ExtractConstantsAnalyzer")
            .WithLocation(0).WithArguments("MyConstant")
            ;
        var expected1 = VerifyCS.Diagnostic("ExtractConstantsAnalyzer")
            .WithLocation(1).WithArguments("123")
            ;
        await VerifyCS.VerifyCodeFixAsync(test, 1, new[] { expected0, expected1 }, fixtest);
    }

    [TestMethod]
    public async Task AddNewULongConstant()
    {
        var test = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
    class MyClass
    {
        public void MyMethod(ulong myarg1)
        {
            if(myarg1 == {|#0:123uL|})
            {
            }
        }
    }
}";

        var fixtest = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
    class MyClass
    {
        private const ulong MyClass123uL = 123UL;
        public void MyMethod(ulong myarg1)
        {
            if(myarg1 == MyClass123uL)
            {
            }
        }
    }
}";

        var expected1 = VerifyCS.Diagnostic("ExtractConstantsAnalyzer")
            .WithLocation(0).WithArguments("123")
            ;
        await VerifyCS.VerifyCodeFixAsync(test, 1, new[] { expected1 }, fixtest);
    }


    [TestMethod]
    public async Task AddNewCharConstant()
    {
        var test = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
    class MyClass
    {
        public void MyMethod(char myarg1)
        {
            if(myarg1 == {|#0:'#'|})
            {
            }
        }
    }
}";

        var fixtest = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
    class MyClass
    {
        private const char MyClass_ = '#';
        public void MyMethod(char myarg1)
        {
            if(myarg1 == MyClass_)
            {
            }
        }
    }
}";

        var expected1 = VerifyCS.Diagnostic("ExtractConstantsAnalyzer")
            .WithLocation(0).WithArguments("#")
            ;
        await VerifyCS.VerifyCodeFixAsync(test, 1, new[] { expected1 }, fixtest);
    }

    [TestMethod]
    public async Task AddNewCharConstantGenericClass()
    {
        var test = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
    class MyClass<T>
    {
        public void MyMethod(char myarg1)
        {
            if(myarg1 == {|#0:'#'|})
            {
            }
        }
    }
}";

        var fixtest = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
    class MyClass<T>
    {
        private const char MyClass_ = '#';
        public void MyMethod(char myarg1)
        {
            if(myarg1 == MyClass_)
            {
            }
        }
    }
}";

        var expected1 = VerifyCS.Diagnostic("ExtractConstantsAnalyzer")
            .WithLocation(0).WithArguments("#")
            ;
        await VerifyCS.VerifyCodeFixAsync(test, 1, new[] { expected1 }, fixtest);
    }


    [TestMethod]
    public async Task AddNewCharConstantNoPaddingLeft()
    {
        var test = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
class MyClass
{
public void MyMethod(char myarg1)
{
if(myarg1 == {|#0:'#'|})
{
}
}
}
}";

        var fixtest = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
class MyClass
{
private const char MyClass_ = '#';
public void MyMethod(char myarg1)
{
if(myarg1 == MyClass_)
{
}
}
}
}";

        var expected1 = VerifyCS.Diagnostic("ExtractConstantsAnalyzer")
            .WithLocation(0).WithArguments("#")
            ;
        await VerifyCS.VerifyCodeFixAsync(test, 1, new[] { expected1 }, fixtest);
    }

    [TestMethod]
    public async Task AddNewCharConstantInline()
    {
        var test = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
class MyClass { public void MyMethod(char myarg1) { if(myarg1 == {|#0:'#'|}) { } } }
}";

        var fixtest = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
class MyClass { private const char MyClass_ = '#';
public void MyMethod(char myarg1) { if(myarg1 == MyClass_) { } } }
}";

        var expected1 = VerifyCS.Diagnostic("ExtractConstantsAnalyzer")
            .WithLocation(0).WithArguments("#")
            ;
        await VerifyCS.VerifyCodeFixAsync(test, 1, new[] { expected1 }, fixtest);
    }
}
