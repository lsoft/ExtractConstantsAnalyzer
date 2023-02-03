# ExtractConstantsAnalyzer

Roslyn analyzer for detection and extraction inline constants into the C# const fields.

From this:

```C#
    class MyClass
    {
        public void MyMethod(string myarg1, int myarg2)
        {
            if(myarg1 == "MyConstant")
            {
            }
            if(myarg2 == 123)
            {
            }
        }
    }
```

to this:

```C#
    class MyClass
    {
        private const int MyClass123 = 123;
        private const string MyClassMyConstant = "MyConstant";

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
```
