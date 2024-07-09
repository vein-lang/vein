namespace veinc_test.Features;

public class QualifiedExpressionTest
{
    public static VeinSyntax VeinAst => new();


    [Test]
    public void NewExpTest()
    {
        var result = VeinAst.QualifiedExpression.End().ParseVein(@"new Foo()");
        IshtarAssert.IsType<NewExpressionSyntax>(result);

        var exp = result as NewExpressionSyntax;
        Assert.NotNull(exp);
        Assert.AreEqual(SyntaxType.NewExpression, exp.Kind);
        IshtarAssert.IsType<ObjectCreationExpression>(exp.CtorArgs);
        Assert.AreEqual("Foo", exp.TargetType.Typeword.GetFullName());
    }
    [Test]
    public void ExpSimplifyTest()
    {
        AppFlags.Set(ApplicationFlag.exp_simplify_optimize, false);
        var result = VeinAst.QualifiedExpression.End().ParseVein("1 + 2 - 3 * 4 / 5 ^^ 2");
        Console.WriteLine(result.ToString());
    }

    [Test]
    public void ReturnExp()
    {
        var result1 = VeinAst.Statement.ParseVein("return;");
        var result2 = VeinAst.Statement.ParseVein("return 1;");
    }

    [Test]
    public void AccessMemberTest() => VeinAst.QualifiedExpression.End().ParseVein(@"44.foo");

    [Test]
    public void ValidateChainMember()
    {
        var key = $"zet.foo()[2]";
        var result = VeinAst.QualifiedExpression.End().ParseVein(key) as AccessExpressionSyntax;

        Assert.NotNull(result);

        IshtarAssert.IsType<IdentifierExpression>(result.Left); // zet
        var indexer = IshtarAssert.IsType<IndexerAccessExpressionSyntax>(result.Right); // foo


        IshtarAssert.IsType<ArgumentListExpression>(indexer.Right); // [5]
        var inv = IshtarAssert.IsType<InvocationExpression>(indexer.Left); // foo()
        IshtarAssert.IsType<IdentifierExpression>(inv.Name); // foo
        Assert.AreEqual("foo", $"{inv.Name}");
    }

    [Test]
    public void GenericExpressionTest() =>
        VeinAst.QualifiedExpression.ParseVein("foo.bar.zoo(zak.woo(a, b, c), a, b, c)");

    [Theory]
    [TestCase("foo()")]
    [TestCase("foo.bar()")]
    [TestCase("foo.bar.zoo()")]
    [TestCase("foo.bar.zoo(a, b)")]
    [TestCase("foo.bar.zoo(a, b, 4)")]
    [TestCase("foo.bar.zoo(a, b, 4, 4 + 4)")]
    [TestCase("foo.bar.zoo(a, b, 4, woo())")]
    [TestCase("foo.bar.zoo(a, b, 4, zak.woo())")]
    [TestCase("foo.bar.zoo(a, b, 4, zak.woo(a, b, 4))")]
    //[TestCase("foo.bar.zoo(a, b, 4, 4 + 4);")]
    public void InvocationTest(string parseStr)
        => VeinAst.QualifiedExpression.End().ParseVein(parseStr);

    [Theory]
    [TestCase("foo);")]
    [TestCase("foo.bar(")]
    [TestCase("foo@bar.zoo(a, b)")]
    [TestCase("foo.bar.zoo(a b, 4)")]
    [TestCase("foo.bar.zoo(a, b, 4, 4 $ 4)")]
    public void InvocationTestFail(string parseStr)
        => Assert.Throws<VeinParseException>(() => VeinAst.QualifiedExpression.End().ParseVein(parseStr));

    [Theory]
    [TestCase("new s()")] // new exp
    [TestCase("new c(1)")] // new exp
    [TestCase("new x(x)")] // new exp
    [TestCase("55.access")] // lit access member
    [TestCase("55 != 55")] // logic exp
    [TestCase("55 << 55")] // math exp
    [TestCase("55 * 55 / 2")] // math exp
    [TestCase("(x: Int32) |> 22")] // function lambda
    [TestCase("(x: Int32) |> {}")] // function lambda
    [TestCase("() |> null")] // function lambda
    [TestCase("22..22")] // range
    [TestCase("x..22")] // range
    [TestCase("22..x")] // range
    [TestCase("true ? 22 : 32")] // conditional exp
    [TestCase("a ? fail new foo() : 2")] // fail exp
    [TestCase("foo ?? 22")] // coalescing exp
    [TestCase("as Type")] // as exp
    [TestCase("is Type")] // is exp
    [TestCase("this.call()")] // this based call
    [TestCase("(1..i < 1 ? 1 : i)")]
    [TestCase("v1.x * v2.x + v1.y * v2.y + v1.z * v2.z")]
    [TestCase("Pf.x - 0.5f + jitter * ox")]
    [TestCase("mod7(floor((p * K))) + K - Ko")]
    [TestCase("(d1.x < d1.y) ? d1.xy : d1.yx")]
    [TestCase("permute(Pi.x + float3(-1.0f, 0.0f, 1.0f))")]
    [TestCase("int4(asint(x.x), asint(x.y), asint(x.z), asint(x.w))")]
    [TestCase("abs(x) < double.PositiveInfinity")]
    [TestCase("(asulong(x) & 07) > 07")]
    [TestCase("double.IsNaN(y) || x < y ? x : y")]
    [TestCase("x = ((x >> 1) & 055555555) | ((x & 055555555) << 1)")]
    [TestCase("uf = select(uf, asuint(asfloat(uf + (1 << 23)) - 6.10351563e-05f), e == 0)")]
    [TestCase("uf += select(0, (128u - 16u) << 23, e == shifted_exp)")]
    //[TestCase("hx = ((asuint(min(asfloat(uux) * 1.92592994e-34f, 260042752.0f)) + 0x1000) >> 13)")]
    [TestCase("basis1.x = 1.0f + sign * normal.x * normal.x * a")]
    [TestCase("hash = rol(state.x, 1) + rol(state.y, 7) + rol(state.z, 12) + rol(state.w, 18)")]
    public void AllExpressionTest(string exp) => VeinAst.QualifiedExpression.End().ParseVein(exp);

    [Theory]
    [TestCase("2 ^ 4 + 2 - 2 * 2 % 2 ^^ 2 == foo()")]
    [TestCase("!106 / ~27 ^ !81 / ~38 ^ ~65 - 202 ^ ~214 & ~143")]
    [TestCase("~16 ^ !243 && ~131 ^ 171 && 224 >> !67 && ~24 * ~235")]
    [TestCase("~107 & 178 ^^ ~111 || ~113 ^^ ~222 >> !100 ^^ 65 || ~94")]
    [TestCase("~119 ^^ ~161 ^^ ~89 * !241 ^^ ~131 && ~129 ^^ 83 ^^ 44")]
    [TestCase("!210 && !96 + ~71 - 34 + ~14 << ~164 + !217 * ~147")]
    [TestCase("222 ^ 78 + !138 & !192 + 211 / ~63 + ~168 ^^ !55")]
    [TestCase("!160 & ~138 ^^ !248 % ~240 ^^ 68 + !206 ^^ !31 || 81")]
    [TestCase("!173 / !120 ^^ 225 && 7 ^^ ~21 >> !238 ^^ ~197 ^^ !98")]
    [TestCase("185 / ~102 / !152 ^ !202 / ~32 ^^ !130 / ~164 & !58")]
    [TestCase("123 + 161 && ~96 * !138 && ~86 & ~158 && !83 ^^ 167")]
    [TestCase("!136 && 144 || 252 * ~123 || ~185 && ~207 || !171 ^ 234")]
    [TestCase("!226 / !79 / 105 * 28 / ~201 && ~205 / ~226 + 64")]
    [TestCase("~164 & !138 && !50 & !212 && 194 >> 34 && 25 * 67")]
    [TestCase("~176 / ~121 || 27 && 234 || 11 & 125 || 238 ^ 70")]
    [TestCase("~189 && !103 || ~4 >> ~2 || 1 - ~195 || !200 && !234")]
    [TestCase("~24 / ~39 / 241 >> 49 / ~127 + !172 / !1 >> ~30")]
    [TestCase("!217 & ~98 && ~186 >> 233 && !42 + 161 && !54 << ~34")]
    [TestCase("!230 / 80 || 18 + 163 || 154 || !192 || !70 && ~173")]
    [TestCase("~242 && ~62 || ~139 ^^ ~23 || !224 >> ~224 || !221 + !228")]
    [TestCase("!180 ^^ 121 % 118 % 85 % 163 / !2 % ~165 << ~73")]
    [TestCase("!193 && ~104 % ~61 ^ !228 % 127 && 23 % 116 / ~236")]
    [TestCase("~139 % !45 & ~130 ^^ 97 & 43 + !47 & 168 / ~84")]
    [TestCase("~230 >> ~236 ^ ~112 * !145 ^ !80 & ~105 ^ 25 << ~173")]
    [TestCase("~242 - !218 ^ ~89 % !168 ^ !112 & ~57 ^ ~14 & !154")]
    [TestCase("~0 << !200 ^ !65 + 190 ^ 140 && ~143 ^ ~143 ^^ ~47")]
    [TestCase("!193 % !5 & !10 % !118 & ~15 ^ !111 & !203 ^ ~28")]
    [TestCase("!205 << ~242 && ~242 + !141 && ~46 ^ !64 && !192 % 9")]
    [TestCase("218 ^ 224 && ~35 - 75 && !238 + !185 && ~137 & !117")]
    [TestCase("!156 >> !28 * 164 / ~92 * !234 % ~204 * ~186 && ~126")]
    [TestCase("!246 - 219 & ~169 << ~146 & !232 & ~238 & !17 << ~190")]
    [TestCase("!4 << !201 && !123 && 162 && 26 % ~14 && ~59 + !83")]
    [TestCase("~196 % 5 / ~15 - 68 / ~232 ^ 152 / !142 / !113")]
    [TestCase("209 & 243 * !251 >> ~129 * ~76 ^^ ~201 * 200 * !153")]
    [TestCase("!222 - ~225 * 21 ^^ ~135 * !250 || !203 * !219 / 101")]
    [TestCase("~57 ^ !161 && ~3 << ~183 && ~236 / !123 && !165 ^ ~144")]
    [TestCase("~250 - 220 * !82 ^ 203 * ~158 << ~119 * !142 << !53")]
    [TestCase("7 << !202 * 196 + ~180 * ~2 ^ ~168 * !230 >> 119")]
    [TestCase("~20 + 184 % !55 && !157 % !217 - !171 % ~185 || ~88")]
    [TestCase("!213 << !243 >> ~102 >> !84 >> !43 ^ ~231 >> !34 << 145")]
    [TestCase("!225 - 226 << !156 ^^ ~79 << 137 >> 74 << 142 - ~204")]
    [TestCase("!163 >> ~30 - 24 / ~35 - !7 << 201 - !2 || !4")]
    [TestCase("~1465335714 + 802595077 - ~1557282100 % ~1701421680")]
    [TestCase("~1571318926 ^^ !654000949 - ~1505232535 * ~1937377465")]
    [TestCase("~185449798 & 110682480 >> 1276287843 || 1353137228")]
    [TestCase("1808804974 % !608506720 % 1161111307 & !891268492")]
    [TestCase("!1914788186 + 459912586 - ~2123293822 << ~695079347")]
    [TestCase("~1390659715 << ~957736826 << ~233210611 / !1322441515")]
    [TestCase("!1496642927 + 809142698 ^ !822816154 % ~37021466")]
    [TestCase("!1602626139 ^ ~660548570 ^ ~1988315968 + !1699505167")]
    [TestCase("~1078497668 - ~1158372810 & 1526447232 || !1096150041")]
    [TestCase("~1184480880 ^ !1009778682 & !1330258087 - !1284681867")]
    [TestCase("~1946095399 + ~466460207 ^ 1178162780 << ~1687924343")]
    [TestCase("2052078611 && !317866079 ^^ !203526744 * 981973635")]
    [TestCase("666209483 >> !1922031251 - ~1107833619 ^^ !829878328")]
    [TestCase("142081012 / !272371844 ^ ~992657083 / ~368009592")]
    [TestCase("!248064224 & ~123777716 ^ !171820447 >> 1864875345")]
    [TestCase("!1871419400 ^ 621601962 & !1839663062 && !1857435358")]
    [TestCase("1977402612 % !473007828 & !654361930 ^ ~1661246213")]
    [TestCase("!2083385824 ^^ ~324413706 && !1465057068 << 1638583869")]
    [TestCase("~1559257353 + ~822237940 * 1003188332 - ~1035228745")]
    [TestCase("!1665240565 ^ !673643812 * ~806999187 >> ~1223760571")]
    [TestCase("-599501569 ^^ -1537610347 ^^ -1801974617 && !-943604673")]
    [TestCase("~-728565646 & ~-1896339527 && !-651565412 && ~-2116790075")]
    public void OperatorTest(string parseKey) =>
            VeinAst.QualifiedExpression.End().ParseVein($"({parseKey})");

    [Theory]
    [TestCase("this.x = 22")]
    [TestCase("this.x.w.d = 22")]
    [TestCase("this.x.w.d = zo.fo()")]
    [TestCase("this.x.w.d = zo.fo(1,2,3)")]
    [TestCase("this.x.w.d = zo.fo(1,2,3, this.x.w.d)")]
    public void ThisAccessTest(string str)
        => VeinAst.QualifiedExpression.End().ParseVein(str);

    [Test]
    public void AssignVariableTest() => VeinAst.QualifiedExpression.End().ParseVein("x = x");
}
