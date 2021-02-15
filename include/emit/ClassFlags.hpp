#pragma once

enum ClassFlags
{
    CLASS_None        = 0 << 0,
    CLASS_Public      = 1 << 0,
    CLASS_Static      = 1 << 1,
    CLASS_Internal    = 1 << 2,
    CLASS_Protected   = 1 << 3,
    CLASS_Private     = 1 << 4
};