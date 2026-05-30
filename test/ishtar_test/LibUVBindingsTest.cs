namespace ishtar_test;

using System;
using System.Runtime.InteropServices;
using ishtar.libuv;
using NUnit.Framework;
using static ishtar.libuv.LibUV;

/// <summary>
/// Tests for libuv P/Invoke bindings to verify struct sizes and basic function calls
/// match the native library (uv.h).
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.None)]
public unsafe class LibUVBindingsTest
{
    [Test]
    public void MutexInitLockUnlockDestroy()
    {
        var mutex = stackalloc byte[sizeof(uv_mutex_t)];
        var m = (uv_mutex_t*)mutex;

        var err = uv_mutex_init(m);
        Assert.That((int)err, Is.EqualTo(0), "uv_mutex_init failed");

        uv_mutex_lock(m);
        uv_mutex_unlock(m);
        uv_mutex_destroy(m);
    }

    [Test]
    public void SemaphoreInitPostWaitDestroy()
    {
        var err = uv_sem_init(out var sem, 0);
        Assert.That((int)err, Is.EqualTo(0), "uv_sem_init failed");

        uv_sem_post(ref sem);
        uv_sem_wait(ref sem);
        uv_sem_destroy(ref sem);
    }

    [Test]
    public void CondInitSignalDestroy()
    {
        var condBuf = stackalloc byte[sizeof(uv_cond_t)];
        var cond = (uv_cond_t*)condBuf;

        var err = uv_cond_init(cond);
        Assert.That((int)err, Is.EqualTo(0), "uv_cond_init failed");

        uv_cond_signal(cond);
        uv_cond_destroy(cond);
    }

    [Test]
    public void CondTimedWaitReturnsTimeout()
    {
        var mutexBuf = stackalloc byte[sizeof(uv_mutex_t)];
        var condBuf = stackalloc byte[sizeof(uv_cond_t)];
        var mutex = (uv_mutex_t*)mutexBuf;
        var cond = (uv_cond_t*)condBuf;

        Assert.That((int)uv_mutex_init(mutex), Is.EqualTo(0));
        Assert.That((int)uv_cond_init(cond), Is.EqualTo(0));

        uv_mutex_lock(mutex);
        // Timeout of 1ms — should return non-zero (UV_ETIMEDOUT)
        var result = uv_cond_timedwait(cond, mutex, 1_000_000); // nanoseconds
        uv_mutex_unlock(mutex);

        // UV_ETIMEDOUT is a negative error code
        Assert.That(result, Is.Not.EqualTo(0), "cond_timedwait should timeout");

        uv_cond_destroy(cond);
        uv_mutex_destroy(mutex);
    }

    [Test]
    public void ThreadCreateAndJoin()
    {
        var called = false;

        void ThreadEntry(nint arg) => called = true;

        var err = uv_thread_create(out var tid, ThreadEntry, nint.Zero);
        Assert.That(err, Is.EqualTo(0), "uv_thread_create failed");

        var joinErr = uv_thread_join(in tid);
        Assert.That(joinErr, Is.EqualTo(0), "uv_thread_join failed");
        Assert.That(called, Is.True, "Thread entry was not called");
    }

    [Test]
    public void ThreadSelfReturnsNonZero()
    {
        var self = uv_thread_self();
        Assert.That(self, Is.Not.EqualTo(nint.Zero), "uv_thread_self returned zero");
    }

    [Test]
    public void DefaultLoopNotNull()
    {
        var loop = uv_default_loop();
        Assert.That(loop, Is.Not.EqualTo(nint.Zero), "uv_default_loop returned null");
    }

    [Test]
    public void LoopNewAndClose()
    {
        var loop = uv_loop_new();
        Assert.That(loop, Is.Not.EqualTo(nint.Zero), "uv_loop_new returned null");

        var closeErr = uv_loop_close(loop);
        Assert.That((int)closeErr, Is.EqualTo(0), "uv_loop_close failed");
    }

    [Test]
    public void LoopSetGetData()
    {
        var loop = uv_loop_new();
        Assert.That(loop, Is.Not.EqualTo(nint.Zero));

        var sentinel = (void*)0xDEADBEEF;
        uv_loop_set_data(loop, sentinel);
        var got = uv_loop_get_data(loop);
        Assert.That((nint)got, Is.EqualTo((nint)sentinel), "loop data round-trip failed");

        uv_loop_close(loop);
    }

    [Test]
    public void KeyCreateSetGetDelete()
    {
        var err = uv_key_create(out var key);
        Assert.That((int)err, Is.EqualTo(0), "uv_key_create failed");

        var value = (void*)0x42;
        uv_key_set(ref key, value);
        var got = uv_key_get(ref key);
        Assert.That(got, Is.EqualTo((nint)value), "uv_key round-trip failed");

        uv_key_delete(ref key);
    }

    [Test]
    public void CpuInfoReturnsPositiveCount()
    {
        uv_cpu_info_t* info;
        var err = uv_cpu_info(&info, out var count);
        Assert.That(err, Is.EqualTo(0), "uv_cpu_info failed");
        Assert.That(count, Is.GreaterThan(0), "cpu count should be > 0");

        uv_free_cpu_info(info, count);
    }

    [Test]
    public void TimerInitAndStop()
    {
        var loop = uv_loop_new();
        // uv_timer_t is an opaque handle; allocate sufficient space
        var timerBuf = stackalloc byte[256]; // libuv timer handle is large
        var timer = (nint)timerBuf;

        var initErr = uv_timer_init(loop, timer);
        Assert.That((int)initErr, Is.EqualTo(0), "uv_timer_init failed");

        var stopErr = uv_timer_stop(timer);
        Assert.That(stopErr, Is.EqualTo(0), "uv_timer_stop failed");

        uv_close(timer, null);
        uv_run(loop, uv_run_mode.UV_RUN_DEFAULT);
        uv_loop_close(loop);
    }

    [Test]
    public void MutexSizeMatchesNative()
    {
        // Verified with native size_check: CRITICAL_SECTION = 40 bytes on Win64
        Assert.That(sizeof(uv_mutex_t), Is.EqualTo(40),
            "uv_mutex_t size mismatch with native CRITICAL_SECTION");
    }

    [Test]
    public void CondSizeMatchesNative()
    {
        // Verified with native size_check: uv_cond_t = 64 bytes on Win64
        Assert.That(sizeof(uv_cond_t), Is.EqualTo(64),
            "uv_cond_t size mismatch with native");
    }

    [Test]
    public void KeySizeMatchesNative()
    {
        // Verified with native size_check: uv_key_t = 4 bytes (DWORD tls_index)
        Assert.That(sizeof(uv_key_t), Is.EqualTo(4),
            "uv_key_t size mismatch with native");
    }

    [Test]
    public void SemSizeMatchesNative()
    {
        // Verified with native size_check: uv_sem_t = HANDLE = 8 bytes on Win64
        Assert.That(sizeof(uv_sem_t), Is.EqualTo(8),
            "uv_sem_t size mismatch with native");
    }

    [Test]
    public void SleepDoesNotCrash()
    {
        // Just verify the binding works without crash
        uv_sleep(1);
    }
}
