#pragma once

enum ClassFlags
{
    CLASS_None        = 0 << 0,
    CLASS_Public      = 1 << 1,
    CLASS_Static      = 1 << 2,
    CLASS_Internal    = 1 << 3,
    CLASS_Protected   = 1 << 4,
    CLASS_Private     = 1 << 5,
    CLASS_Abstract    = 1 << 6,
};