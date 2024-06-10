namespace ishtar.vm.libuv;

using System.Runtime.InteropServices;


public static class LibUV
{
    public const string LIBNAME = "libuv";

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int uv_thread_create(out uv_thread_t tid, uv_thread_cb entry, IntPtr arg);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int uv_thread_join(uv_thread_t tid);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern nint uv_loop_new();

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int uv_loop_init(nint loop);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int uv_run(nint loop, uv_run_mode mode);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void uv_stop(nint loop);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int uv_async_init(nint loop, nint handle, uv_async_cb cb);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int uv_async_send(nint handle);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void uv_close(nint handle, uv_close_cb cb);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong uv_thread_self();

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern nint uv_default_loop();
    
    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int uv_timer_init(nint loop, nint handle);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int uv_timer_start(nint handle, uv_timer_cb cb, ulong timeout, ulong repeat);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void uv_timer_stop(nint handle);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int uv_queue_work(nint loop, ref uv_work_t req, uv_work_cb work_cb, uv_after_work_cb after_work_cb);
    
    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int uv_sem_init(out uv_sem_t sem, int value);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void uv_sem_post(ref uv_sem_t sem);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void uv_sem_wait(ref uv_sem_t sem);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void uv_sem_destroy(ref uv_sem_t sem);

    public delegate void uv_async_cb(nint handle);
    public delegate void uv_close_cb(nint handle);
    public delegate void uv_timer_cb(nint handle);
    public delegate void uv_thread_cb(nint arg);
    public delegate void uv_after_work_cb(nint req, int status);
    public delegate void uv_work_cb(nint req);


    public enum uv_run_mode
    {
        UV_RUN_DEFAULT = 0,
        UV_RUN_ONCE = 1,
        UV_RUN_NOWAIT = 2
    }
    public enum uv_loop_option
    {
        UV_LOOP_BLOCK_SIGNAL = 0,
        UV_METRICS_IDLE_TIME = 1
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct uv_thread_t
    {
        private nint handle;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct uv_sem_t
    {
        private nint handle;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct uv_work_t
    {
        private nint handle;
    }
}
