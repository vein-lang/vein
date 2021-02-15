#pragma once

enum FieldFlags 
{
    FIELD_None = 0,
    FIELD_Literal = 1 << 1,
    FIELD_Public = 1 << 2,
    FIELD_Static = 1 << 3,
    FIELD_Protected = 1 << 4
};