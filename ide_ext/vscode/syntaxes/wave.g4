grammar wave;

INTEGER
    :    '-'? ('0'..'9')+
    ;
FLOAT
    :    '-'? ('0'..'9')+ '.' ('0'..'9')+
    ;

STRING
     :    '\'' (~ '\'' )* '\''
     ;
BOOLEAN
    :    'true'
    |    'false'
    ;
r  : 'hello' ID ;         // match keyword hello followed by an identifier
ID : [a-z]+ ;             // match lower-case identifiers
WS : [ \t\r\n]+ -> skip ; // skip spaces, tabs, newlines