parser grammar WaveParser;
options 
{ 
	tokenVocab=WaveLexer;
    language=CSharp; 
}

compilation_unit
	: BYTE_ORDER_MARK? extern_directives? use_directives?
	  global_attribute_section* namespace_member_declarations? EOF
	;

body
	: block
	| ';'
	;

namespace_or_type_name 
	: (identifier type_argument_list? | qualified_alias_member) ('.' identifier type_argument_list?)*
	;

qualified_alias_member
	: identifier '::' identifier type_argument_list?
	;
extern_directives
	: extern_directive+
	;
extern_directive
	: EXTERN identifier ';'
	;
use_directives
	: use_directive+
	;

use_directive
	: USE namespace_or_type_name ';'                         #useNamespaceDirective
	| USE STATIC namespace_or_type_name ';'                  #useStaticDirective
	;

global_attribute_section
	: '[' global_attribute_target ':' attribute_list ','? ']'
	;

global_attribute_target
	: keyword
	| identifier
	;

attribute_list
	: attribute (','  attribute)*
	;

attribute
	: namespace_or_type_name (OPEN_PARENS (attribute_argument (','  attribute_argument)*)? CLOSE_PARENS)?
	;

attribute_argument
	: (identifier ':')? expression
	;

attributes
	: attribute_section+
	;

attribute_section
	: '[' (attribute_target ':')? attribute_list ','? ']'
	;

attribute_target
	: keyword
	| identifier
	;

namespace_member_declarations
	: namespace_member_declaration+
	;

namespace_member_declaration
	: namespace_declaration
	| type_declaration
	;

type_declaration
	: attributes? all_member_modifiers?
      (class_definition | struct_definition | interface_definition | enum_definition)
  ;

namespace_declaration
	: SPACE qi=qualified_identifier namespace_body ';'?
	;

namespace_body
	: OPEN_BRACE use_directives? namespace_member_declarations? CLOSE_BRACE
	;

qualified_identifier
	: identifier ( '.'  identifier )*
	;

type_parameter_list
	: '<' type_parameter (','  type_parameter)* '>'
	;

type_parameter
	: attributes? identifier
	;

// modifications

all_member_modifiers
	: all_member_modifier+
	;

all_member_modifier
	: NEW
	| PUBLIC
	| PROTECTED
	| INTERNAL
	| PRIVATE
	| READONLY
	| VIRTUAL
	| OVERRIDE
	| ABSTRACT
	| STATIC
	| EXTERN
	;
// definitions


common_member_declaration
	: constant_declaration
	| typed_member_declaration
	| constructor_declaration
	| VOID method_declaration
	| class_definition
	| struct_definition
	| interface_definition
	| enum_definition
	;

typed_member_declaration
	: (READONLY)? type_
	  ( namespace_or_type_name '.' indexer_declaration
	  | method_declaration
	  | indexer_declaration
	  | field_declaration
	  )
	;

class_definition
	: CLASS identifier 
	type_parameter_list? 
	class_base? 
	type_parameter_constraints_clauses?
	    class_body ';'?
	;

struct_definition
	: (READONLY)? STRUCT identifier type_parameter_list? struct_interfaces? type_parameter_constraints_clauses?
	    struct_body ';'?
	;


interface_definition
	: INTERFACE identifier variant_type_parameter_list? interface_base?
	    type_parameter_constraints_clauses? class_body ';'?
	;

enum_definition
	: ENUM identifier enum_base? enum_body ';'?
	;

interface_type_list
	: namespace_or_type_name (','  namespace_or_type_name)*
	;

field_declaration
	: variable_declarators ';'
	;

variable_declarators
	: variable_declarator (','  variable_declarator)*
	;

variable_declarator
	: identifier ('=' variable_initializer)?
	;

variable_initializer
	: expression
	| array_initializer
	;

type_parameter_constraints_clauses
	: type_parameter_constraints_clause+
	;

type_parameter_constraints_clause
	: WHERE identifier ':' type_parameter_constraints
	;

type_parameter_constraints
	: constructor_constraint
	| primary_constraint (',' secondary_constraints)? (',' constructor_constraint)?
	;

secondary_constraints
	: namespace_or_type_name (',' namespace_or_type_name)*
	;

constructor_constraint
	: NEW OPEN_PARENS CLOSE_PARENS
	;

primary_constraint
	: class_type
	| CLASS '?'?
	| STRUCT
	| NUMBER
	;

// enums

enum_base
	: ':' type_
	;

enum_body
	: OPEN_BRACE (enum_member_declaration (','  enum_member_declaration)* ','?)? CLOSE_BRACE
	;

enum_member_declaration
	: attributes? identifier ('=' expression)?
	;

// classes

class_base
	: ':' class_type (','  namespace_or_type_name)*
	;

class_body
	: OPEN_BRACE class_member_declarations? CLOSE_BRACE
	;

class_member_declarations
	: class_member_declaration+
	;

class_member_declaration
	: attributes? all_member_modifiers? (common_member_declaration | destructor_definition)
	;

// interfaces

variant_type_parameter_list
	: '<' variant_type_parameter (',' variant_type_parameter)* '>'
	;

variant_type_parameter
	: attributes? identifier
	;

interface_base
	: ':' interface_type_list
	;

interface_body // ignored in csharp 8
	: OPEN_BRACE interface_member_declaration* CLOSE_BRACE
	;

interface_member_declaration
	: attributes? NEW?
	  (READONLY? type_
	    ( identifier type_parameter_list? OPEN_PARENS formal_parameter_list? CLOSE_PARENS type_parameter_constraints_clauses? ';'
	    | identifier OPEN_BRACE interface_accessors CLOSE_BRACE
	    | THIS '[' formal_parameter_list ']' OPEN_BRACE interface_accessors CLOSE_BRACE)
	  | VOID identifier type_parameter_list? OPEN_PARENS formal_parameter_list? CLOSE_PARENS type_parameter_constraints_clauses? ';')
	;

interface_accessors
	: attributes? (GETTER ';' (attributes? SETTER ';')? | SETTER ';' (attributes? GETTER ';')?)
	;

// structs

struct_interfaces
	: ':' interface_type_list
	;

struct_body
	: OPEN_BRACE struct_member_declaration* CLOSE_BRACE
	;

struct_member_declaration
	: attributes? all_member_modifiers?
	  (common_member_declaration)
	;

// declarations

constant_declarator
	: identifier '=' expression
	;

constant_declarators
	: constant_declarator (','  constant_declarator)*
	;

constant_declaration
	: CONST type_ constant_declarators ';'
	;

indexer_declaration
	: THIS '[' formal_parameter_list ']' (OPEN_BRACE accessor_declarations CLOSE_BRACE | right_arrow failable_expression ';')
	;

accessor_declarations
	: attrs=attributes? mods=accessor_modifier?
	  (GETTER accessor_body set_accessor_declaration? | SETTER accessor_body get_accessor_declaration?)
	;

accessor_modifier
	: PROTECTED
	| INTERNAL
	| PRIVATE
	| PROTECTED INTERNAL
	| INTERNAL PROTECTED
	;

get_accessor_declaration
	: attributes? accessor_modifier? GETTER accessor_body
	;

set_accessor_declaration
	: attributes? accessor_modifier? SETTER accessor_body
	;

accessor_body
	: block
	| ';'
	;

formal_parameter_list
	: parameter_array
	| fixed_parameters (',' parameter_array)?
	;

fixed_parameters
	: fixed_parameter ( ',' fixed_parameter )*
	;

fixed_parameter
	: attributes? parameter_modifier? arg_declaration
	;

parameter_modifier
	: IN
	| IN THIS
	| THIS
	;

parameter_array
	: attributes? PARAMS array_type identifier
	;

destructor_definition
	: DELETE OPEN_PARENS CLOSE_PARENS body
	;

constructor_declaration
	: NEW OPEN_PARENS formal_parameter_list? CLOSE_PARENS constructor_initializer? body
	;
constructor_initializer
	: ':' (BASE | THIS) OPEN_PARENS argument_list? CLOSE_PARENS
	;

method_declaration
	: method_member_name type_parameter_list? OPEN_PARENS formal_parameter_list? CLOSE_PARENS
	    type_parameter_constraints_clauses? (method_body | right_arrow failable_expression ';')
	;

method_member_name
	: (identifier | identifier '::' identifier) (type_argument_list? '.' identifier)*
	;

arg_declaration
	: type_ identifier ('=' expression)?
	;

object_creation_expression
	: OPEN_PARENS argument_list? CLOSE_PARENS object_or_collection_initializer?
	;

method_invocation
	: OPEN_PARENS argument_list? CLOSE_PARENS
	;

// types

floating_point_type 
	: FLOAT
	| DOUBLE
    | HALF
    | DECIMAL
	;

base_type
	: simple_type
	| class_type
	| VOID '*'
	;
simple_type 
	: numeric_type
	| BOOL
	;

numeric_type 
	: integral_type
	| floating_point_type
	| DECIMAL
	;

integral_type 
	: INT8
	| UINT8
	| INT16 
    | UINT16 
    | INT32 
    | UINT32 
    | INT64 
    | UINT64
	| CHAR
	;

type_argument_list 
	: '<' type_ ( ',' type_)* '>'
	;

class_type 
	: namespace_or_type_name
	| OBJECT
	| STRING
	;

type_
	: base_type ('?' | rank_specifier | '*')*
	;

// arrays

array_type
	: base_type (('*' | '?')* rank_specifier)+
	;

rank_specifier
	: '[' ','* ']'
	;

array_initializer
	: OPEN_BRACE (variable_initializer (','  variable_initializer)* ','?)? CLOSE_BRACE
	;


// literals

right_arrow
	: first='=' second='>' {$first.index + 1 == $second.index}?
	;

right_shift
	: first='>' second='>' {$first.index + 1 == $second.index}?
	;

right_shift_assignment
	: first='>' second='>=' {$first.index + 1 == $second.index}?
	;

boolean_literal
	: TRUE
	| FALSE
	;

string_literal
	: interpolated_regular_string
	| REGULAR_STRING
	;
interpolated_string_expression
	: expression (',' expression)* (':' FORMAT_STRING+)?
	;
interpolated_regular_string_part
	: interpolated_string_expression
	| DOUBLE_CURLY_INSIDE
	| REGULAR_CHAR_INSIDE
	| REGULAR_STRING_INSIDE
	;
interpolated_regular_string
	: INTERPOLATED_REGULAR_STRING_START interpolated_regular_string_part* DOUBLE_QUOTE_INSIDE
	;

literal
	: boolean_literal
	| string_literal
	| INTEGER_LITERAL
	| HEX_INTEGER_LITERAL
	| BIN_INTEGER_LITERAL
	| REAL_LITERAL
	| CHARACTER_LITERAL
	| NULL
	;

// expression

argument_list 
	: argument ( ',' argument)*
	;

argument
	: (identifier ':')? (AUTO | type_)? expression
	;

expression
	: assignment
	| non_assignment_expression
	;

non_assignment_expression
	: lambda_expression
	| conditional_expression
	;

assignment
	: unary_expression assignment_operator expression
	| unary_expression '??=' failable_expression
	;

assignment_operator
	: '=' | '+=' | '-=' | '*=' | '/=' | '%=' | '&=' | '|=' | '^=' | '<<=' | right_shift_assignment
	;

conditional_expression
	: null_coalescing_expression ('?' failable_expression ':' failable_expression)?
	;

null_coalescing_expression
	: conditional_or_expression ('??' (null_coalescing_expression | fail_expression))?
	;

conditional_or_expression
	: conditional_and_expression (OP_OR conditional_and_expression)*
	;

conditional_and_expression
	: inclusive_or_expression (OP_AND inclusive_or_expression)*
	;

inclusive_or_expression
	: exclusive_or_expression ('|' exclusive_or_expression)*
	;

exclusive_or_expression
	: and_expression ('^' and_expression)*
	;

and_expression
	: equality_expression ('&' equality_expression)*
	;

equality_expression
	: relational_expression ((OP_EQ | OP_NE)  relational_expression)*
	;

relational_expression
	: shift_expression (('<' | '>' | '<=' | '>=') shift_expression | IS isType | AS type_)*
	;

shift_expression
	: additive_expression (('<<' | right_shift)  additive_expression)*
	;

additive_expression
	: multiplicative_expression (('+' | '-')  multiplicative_expression)*
	;

multiplicative_expression
	: switch_expression (('*' | '/' | '%')  switch_expression)*
	;

switch_expression
    : range_expression ('switch' '{' (switch_expression_arms ','?)? '}')?
    ;

switch_expression_arms
    : switch_expression_arm (',' switch_expression_arm)*
    ;

switch_expression_arm
    : expression case_guard? right_arrow failable_expression
    ;

range_expression
    : unary_expression
    | unary_expression? OP_RANGE unary_expression?
    ;

unary_expression
	: primary_expression
	| '+' unary_expression
	| '-' unary_expression
	| '!' unary_expression
	| '~' unary_expression
	| '++' unary_expression
	| '--' unary_expression
	| '(' type_ ')' unary_expression
	| '&' unary_expression
	| '*' unary_expression
	;

primary_expression 
	: pe=primary_expression_start '!'? bracket_expression* '!'?
	  (((member_access | method_invocation | '++' | '--' | '->' identifier) '!'?) bracket_expression* '!'?)*
	;

foo : OPEN_PARENS*;

primary_expression_start
	: literal                                   #literalExpression
	| identifier type_argument_list?            #simpleNameExpression
	| OPEN_PARENS expression CLOSE_PARENS       #parenthesisExpressions
	| predefined_type                           #memberAccessExpression
	| qualified_alias_member                    #memberAccessExpression
	| LITERAL_ACCESS                            #literalAccessExpression
	| THIS                                      #thisReferenceExpression
	| BASE ('.' identifier type_argument_list? | '[' expression_list ']') #baseAccessExpression
	| NEW (type_ (object_creation_expression
	             | object_or_collection_initializer
	             | '[' expression_list ']' rank_specifier* array_initializer?
	             | rank_specifier+ array_initializer)
	      | anonymous_object_initializer
	      | rank_specifier array_initializer)                       #objectCreationExpression
	| TYPEOF OPEN_PARENS (unbound_type_name | type_ | VOID) CLOSE_PARENS   #typeofExpression
	| SIZEOF OPEN_PARENS type_ CLOSE_PARENS                          #sizeofExpression
	| NAMEOF OPEN_PARENS (identifier '.')* identifier CLOSE_PARENS  #nameofExpression
	;

failable_expression
	: expression
	| fail_expression
	;

fail_expression
	: FAIL expression
	;

member_access
	: '?'? '.' identifier type_argument_list?
	;

bracket_expression
	: '?'? '[' indexer_argument ( ',' indexer_argument)* ']'
	;

indexer_argument
	: (identifier ':')? expression
	;
predefined_type
	: BOOL | INT8 | UINT8 | CHAR | DECIMAL | DOUBLE | FLOAT | HALF | INT16 | UINT16
	| INT32 | UINT32 | UINT64 | INT64 | OBJECT | STRING
	;

expression_list
	: expression (',' expression)*
	;

object_or_collection_initializer
	: object_initializer
	| collection_initializer
	;

object_initializer
	: OPEN_BRACE (member_initializer_list ','?)? CLOSE_BRACE
	;

member_initializer_list
	: member_initializer (',' member_initializer)*
	;

member_initializer
	: (identifier | '[' expression ']') '=' initializer_value
	;

initializer_value
	: expression
	| object_or_collection_initializer
	;

collection_initializer
	: OPEN_BRACE element_initializer (',' element_initializer)* ','? CLOSE_BRACE
	;

element_initializer
	: non_assignment_expression
	| OPEN_BRACE expression_list CLOSE_BRACE
	;

anonymous_object_initializer
	: OPEN_BRACE (member_declarator_list ','?)? CLOSE_BRACE
	;

member_declarator_list
	: member_declarator ( ',' member_declarator)*
	;

member_declarator
	: primary_expression
	| identifier '=' expression
	;

unbound_type_name
	: identifier ( generic_dimension_specifier? | '::' identifier generic_dimension_specifier?)
	  ('.' identifier generic_dimension_specifier?)*
	;

generic_dimension_specifier
	: '<' ','* '>'
	;


isType
	: base_type (rank_specifier | '*')* '?'? isTypePatternArms? identifier?
	;

isTypePatternArms
	: '{' isTypePatternArm (',' isTypePatternArm)* '}'
	;

isTypePatternArm
	: identifier ':' expression
	;

lambda_expression
	: anonymous_function_signature right_arrow anonymous_function_body
	;

anonymous_function_signature
	: OPEN_PARENS CLOSE_PARENS
	| OPEN_PARENS explicit_anonymous_function_parameter_list CLOSE_PARENS
	| OPEN_PARENS implicit_anonymous_function_parameter_list CLOSE_PARENS
	| identifier
	;

explicit_anonymous_function_parameter_list
	: explicit_anonymous_function_parameter ( ',' explicit_anonymous_function_parameter)*
	;

explicit_anonymous_function_parameter
	: type_ identifier
	;

implicit_anonymous_function_parameter_list
	: identifier (',' identifier)*
	;

anonymous_function_body
	: failable_expression
	| block
	;


// statements


statement
	: labeled_Statement
	| declarationStatement
	| embedded_statement
	;

declarationStatement
	: local_variable_declaration ';'
	| local_constant_declaration ';'
	;

labeled_Statement
	: identifier ':' statement  
	;

embedded_statement
	: block
	| simple_embedded_statement
	;

simple_embedded_statement
	: ';'                                                         #theEmptyStatement
	| expression ';'                                              #expressionStatement

	| IF OPEN_PARENS expression CLOSE_PARENS if_body (ELSE if_body)?                                        #ifStatement
    | SWITCH OPEN_PARENS expression CLOSE_PARENS OPEN_BRACE switch_section* CLOSE_BRACE                     #switchStatement

	| WHILE OPEN_PARENS expression CLOSE_PARENS embedded_statement                                          #whileStatement
	| DO embedded_statement WHILE OPEN_PARENS expression CLOSE_PARENS ';'                                   #doStatement
	| FOR OPEN_PARENS for_initializer? ';' expression? ';' for_iterator? CLOSE_PARENS embedded_statement    #forStatement
	| FOREACH OPEN_PARENS local_variable_type identifier IN expression CLOSE_PARENS embedded_statement      #foreachStatement

	| BREAK ';'                                                   #breakStatement
	| CONTINUE ';'                                                #continueStatement
	| RETURN expression? ';'                                      #returnStatement
	| FAIL expression? ';'                                        #failStatement

	| TRY block (catch_clauses finally_clause? | finally_clause)  #tryStatement
	| USE OPEN_PARENS resource_acquisition CLOSE_PARENS embedded_statement       #useStatement
	;

block
	: OPEN_BRACE statement_list? CLOSE_BRACE
	;

local_variable_declaration
	: (USE | READONLY)? local_variable_type local_variable_declarator ( ','  local_variable_declarator)*
	;

local_variable_type 
	: AUTO
	| type_
	;

local_variable_declarator
	: identifier ('=' local_variable_initializer)?
	;

local_variable_initializer
	: expression
	| array_initializer
	;

local_constant_declaration
	: CONST type_ constant_declarators
	;

if_body
	: block
	| simple_embedded_statement
	;

switch_section
	: switch_label+ statement_list
	;

switch_label
	: CASE expression case_guard? ':'
	| DEFAULT ':'
	;

case_guard
	: WHEN expression
	;

statement_list
	: statement+
	;

for_initializer
	: local_variable_declaration
	| expression (','  expression)*
	;

for_iterator
	: expression (','  expression)*
	;

catch_clauses
	: specific_catch_clause (specific_catch_clause)* general_catch_clause?
	| general_catch_clause
	;

specific_catch_clause
	: CATCH OPEN_PARENS class_type identifier? CLOSE_PARENS exception_filter? block
	;

general_catch_clause
	: CATCH exception_filter? block
	;

exception_filter
	: WHEN OPEN_PARENS expression CLOSE_PARENS
	;

finally_clause
	: FINALLY block
	;

resource_acquisition
	: local_variable_declaration
	| expression
	;

// body

return_type
	: type_
	| VOID
	;

method_body
	: block
	| ';'
	;

operation_body
	: block
	| ';'
	;

// etc

keyword
	: ABSTRACT
	| AS
	| BASE
	| BOOL
	| BREAK
	| CASE
	| CATCH
	| CHAR
	| CLASS
	| CONST
	| CONTINUE
	| DECIMAL
	| DEFAULT
	| DO
	| DOUBLE
	| ELSE
	| ENUM
	| EXTERN
	| FALSE
	| FINALLY
	| FLOAT
	| FOR
	| FOREACH
	| IF
	| IN
	| INTERFACE
	| INTERNAL
	| IS
	| SPACE
	| NEW
	| NULL
	| OBJECT
	| OVERRIDE
	| PRIVATE
	| PROTECTED
	| PUBLIC
	| READONLY
	| RETURN
	| SIZEOF
	| STATIC
	| STRING
	| STRUCT
	| SWITCH
	| THIS
	| FAIL
	| TRUE
	| TRY
	| TYPEOF
	| USE
	| VIRTUAL
	| VOID
	| WHILE
    | INT8 
    | UINT8 
    | INT16 
    | UINT16 
    | INT32 
    | UINT32 
    | INT64 
    | UINT64 
    | HALF 
    | FLOAT 
    | DOUBLE 
    | DECIMAL 
    | OBJECT 
    | STRING
	;

identifier
	: IDENTIFIER
	| NAMEOF
	| AUTO
	| WHEN
	;