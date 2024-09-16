namespace ishtar_test;

[TestFixture(Ignore = "temporary disabled")]
public unsafe class ReflectionTest : IshtarTestBase
{
   //// [Test]
   // public void AllocateType()
   // {
   //     var ctx = CreateContext();
   //     var frame = ctx.Execute((generator, o) => generator.Emit(OpCodes.RET));
   //     var obj = GC->AllocTypeInfoObject(T.STRING, frame);
   //     GC->FreeObject(&obj, frame);
   // }

   //// [Test]
   // public void AllocateField()
   // {
   //     var ctx = CreateContext();
   //     var frame = ctx.Execute((generator, o) => generator.Emit(OpCodes.RET));
   //     var obj = GC->AllocFieldInfoObject(T.STRING.Field["Length"], frame);
   //     new IshtarLayerField(obj, frame);
   //     GC->FreeObject(&obj, frame);
   // }

   //// [Test]
   // public void AllocateMethod()
   // {
   //     var ctx = CreateContext();
   //     var frame = ctx.Execute((generator, o) => generator.Emit(OpCodes.RET));
   //     var obj = GC->AllocMethodInfoObject(T.STRING.Method["op_Equal(String,String)"], frame);
   //     var fn = new IshtarLayerFunction(obj, frame);
   //     Equals("op_Equal", fn.Name);
   //     GC->FreeObject(&obj, frame);
   // }
}
