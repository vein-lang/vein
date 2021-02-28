#pragma once


#ifdef __x86_64__
#ifndef _MSC_VER
static inline void memory_barrier (void)
{
	__asm__ __volatile__ ("mfence" : : : "memory");
}

static inline void memory_read_barrier (void)
{
	__asm__ __volatile__ ("lfence" : : : "memory");
}

static inline void memory_write_barrier (void)
{
	__asm__ __volatile__ ("sfence" : : : "memory");
}
#else
#include <intrin.h>

static inline void memory_barrier (void)
{
	_ReadWriteBarrier ();
}

static inline void memory_read_barrier (void)
{
	_ReadBarrier ();
}

static inline void memory_write_barrier (void)
{
	_WriteBarrier ();
}
#endif
#elif defined(__i386__)
#ifndef _MSC_VER
static inline void memory_barrier (void)
{
	__asm__ __volatile__ ("lock; addl $0,0(%%esp)" : : : "memory");
}

static inline void memory_read_barrier (void)
{
	memory_barrier ();
}

static inline void memory_write_barrier (void)
{
	memory_barrier ();
}
#else
#include <intrin.h>

static inline void memory_barrier (void)
{
	_ReadWriteBarrier ();
}

static inline void memory_read_barrier (void)
{
	_ReadBarrier ();
}

static inline void memory_write_barrier (void)
{
	_WriteBarrier ();
}
#endif
#elif defined(sparc) || defined(__sparc__)
static inline void memory_barrier (void)
{
	__asm__ __volatile__ ("membar	#LoadLoad | #LoadStore | #StoreStore | #StoreLoad" : : : "memory");
}

static inline void memory_read_barrier (void)
{
	__asm__ __volatile__ ("membar	#LoadLoad" : : : "memory");
}

static inline void memory_write_barrier (void)
{
	__asm__ __volatile__ ("membar	#StoreStore" : : : "memory");
}
#elif defined(__s390__)
static inline void memory_barrier (void)
{
	__asm__ __volatile__ ("bcr 15,0" : : : "memory");
}

static inline void memory_read_barrier (void)
{
	memory_barrier ();
}

static inline void memory_write_barrier (void)
{
	memory_barrier ();
}
#elif defined(__ppc__) || defined(__powerpc__) || defined(__ppc64__)
static inline void memory_barrier (void)
{
	__asm__ __volatile__ ("sync" : : : "memory");
}

static inline void memory_read_barrier (void)
{
	memory_barrier ();
}

static inline void memory_write_barrier (void)
{
	__asm__ __volatile__ ("eieio" : : : "memory");
}

#elif defined(__arm__)
static inline void memory_barrier (void)
{
	__asm__ __volatile__ ("" : : : "memory");
}

static inline void memory_read_barrier (void)
{
	memory_barrier ();
}

static inline void memory_write_barrier (void)
{
	memory_barrier ();
}
#elif defined(__ia64__)
static inline void memory_barrier (void)
{
	__asm__ __volatile__ ("mf" : : : "memory");
}

static inline void memory_read_barrier (void)
{
	memory_barrier ();
}

static inline void memory_write_barrier (void)
{
	memory_barrier ();
}
#elif defined(__alpha__)
static inline void memory_barrier (void)
{
    __asm__ __volatile__ ("mb" : : : "memory");
}

static inline void memory_read_barrier (void)
{
    memory_barrier ();
}

static inline void memory_write_barrier (void)
{
    memory_barrier ();
}
#elif defined(__mips__)
static inline void memory_barrier (void)
{
    __asm__ __volatile__ ("" : : : "memory");
}

static inline void memory_read_barrier (void)
{
    memory_barrier ();
}

static inline void memory_write_barrier (void)
{
    memory_barrier ();
}
#endif

