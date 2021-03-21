namespace wave.syntax
{
    using Antlr4.Runtime.Tree;
    using lexer;
    using static wave.lexer.WaveParser;

    public class WaveSyntaxVisitor : WaveParserBaseVisitor<ExpressionSyntax>
    {
        public override ExpressionSyntax VisitClass_definition(Class_definitionContext context)
        {
            var r = base.VisitClass_definition(context);
            return r;
        }

        public override ExpressionSyntax VisitAll_member_modifiers(All_member_modifiersContext context)
        {
            return base.VisitAll_member_modifiers(context);
        }

        public override ExpressionSyntax VisitAll_member_modifier(All_member_modifierContext context)
        {
            return base.VisitAll_member_modifier(context);
        }

        public override ExpressionSyntax VisitAttributes(AttributesContext context)
        {
            return base.VisitAttributes(context);
        }

        public override ExpressionSyntax VisitAttribute(AttributeContext context)
        {
            return base.VisitAttribute(context);
        }

        public override ExpressionSyntax VisitType_declaration(Type_declarationContext context)
        {
            return base.VisitType_declaration(context);
        }

        public override ExpressionSyntax VisitIdentifier(IdentifierContext context)
        {
            var x = context.IDENTIFIER();
            
            return base.VisitIdentifier(context);
        }
        
        public override ExpressionSyntax VisitClass_base(Class_baseContext context)
        {
            return base.VisitClass_base(context);
        }

        public override ExpressionSyntax VisitAccessor_declarations(Accessor_declarationsContext context)
        {
            return base.VisitAccessor_declarations(context);
        }

        public override ExpressionSyntax VisitClass_body(Class_bodyContext context)
        {
            return base.VisitClass_body(context);
        }

        public override ExpressionSyntax VisitClass_member_declaration(Class_member_declarationContext context)
        {
            return base.VisitClass_member_declaration(context);
        }

        public override ExpressionSyntax VisitClass_member_declarations(Class_member_declarationsContext context)
        {
            return base.VisitClass_member_declarations(context);
        }

        public override ExpressionSyntax VisitCommon_member_declaration(Common_member_declarationContext context)
        {
            return base.VisitCommon_member_declaration(context);
        }

        public override ExpressionSyntax VisitMethod_declaration(Method_declarationContext context)
        {
            return base.VisitMethod_declaration(context);
        }
    }
}