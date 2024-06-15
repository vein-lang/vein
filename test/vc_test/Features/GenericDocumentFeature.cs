namespace veinc_test.Features;

public class GenericDocumentFeature
{
    public static VeinSyntax Syntax = new();

    [Test]
    public void test1()
    {
        Syntax.CompilationUnit.ParseVein(
            """
            #space "test"
            #use "vein/lang"
            
            struct App {
                public test2(): void
                {
                    for (let i = 55; i++; i != 500) {
                        Out.print("hello world!");
                    }
                }
                public test1(): void {
                    fail null;
                }
            
                public test1(): void {
                    let b = "asdads" + "gfdfg";
                }
            
                public static master(): void {
                    Out.print(Fib(15));
                }
            
                public static Fib(n: i32): i32
                {
                    if (n < 2)
                    {
                        return n;
                    }
                    auto a = Fib(n - 1);
                    auto b = Fib(n - 2)
                    return a + b;
                }
            }
            """
        );
    }

    [Test]
    public void test2()
    {
        Syntax.CompilationUnit.ParseVein(
            """
            #space "test"
            #use "vein/lang"

            struct App {
                public test1(): i32 { return 1; }
                public test2(): void { }
                public test3(): boolean { return false; }
                public test4(): string { return "test"; }
                public test5(): void { }
                public test6(): void { }
            }
            """
        );
    }

    [Test]
    public void test3()
    {
        Syntax.CompilationUnit.ParseVein(
            """
            #space "test"
            #use "vein/lang"

            struct App {
                public test1(): i32 { fail null; }
                public test2(): void { }
                public test3(): boolean { 
                    if (true) {
                        return true;
                    } else {
                        return false;
                    }
                }
                public test4(): string { return "test"; }
                public test5(i: i32): void { }
                public test6(b: string): void { }
            }
            """
        );
    }

    [Test]
    public void test4()
    {
        Syntax.CompilationUnit.ParseVein(
            """
            #space "test"
            #use "vein/lang"
            
            struct App 
            {
                public test1(): void 
                {
                    let b = "asdads" + "gfdfg";
                }
            
                public static master(): void {
                    Out.print(Fib(15));
                }
            
                public static Fib(n: i32): i32
                {
                    if (n < 2)
                    {
                        return n;
                    }
                    auto a = Fib(n - 1);
                    auto b = Fib(n - 2)
                    return a + b;
                }
            }
            """
        );
    }
}
