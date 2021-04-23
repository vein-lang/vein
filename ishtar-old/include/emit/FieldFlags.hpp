#pragma once

enum FieldFlags 
{
    FIELD_None = 0,
    FIELD_Literal = 1 << 1,
    FIELD_Public = 1 << 2,
    FIELD_Static = 1 << 3,
    FIELD_Protected = 1 << 4,
    FIELD_Virtual = 1 << 5,
    FIELD_Abstract = 1 << 6,
    FIELD_Override = 1 << 7
};