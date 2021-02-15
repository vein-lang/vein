#pragma once

enum FieldFlags 
{
    None = 0,
    Literal = 1 << 1,
    Public = 1 << 2,
    Static = 1 << 3,
    Protected = 1 << 4
};