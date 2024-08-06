namespace ishtar.libuv;

using System.Runtime.InteropServices;
public static unsafe class LibUV
{
    public const string LIBNAME = "libuv";

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int uv_thread_create(out uv_thread_t tid, uv_thread_cb entry, IntPtr arg);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
    public static extern int uv_thread_join([In]in uv_thread_t tid);

    //

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern nint uv_loop_new();

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int uv_run(nint loop, uv_run_mode mode);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void uv_stop(nint loop);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int uv_async_init(nint loop, nint handle, uv_async_cb asyncCallback);
    [StructLayout(LayoutKind.Sequential)]
    public struct uv_async_t
    {
        public void* data;
        public void* loop;
    }
    //[StructLayout(LayoutKind.Sequential)]
    //public struct uv_loop_t
    //{
    //    public void* data;
    //    public uint active_handles;
    //}


    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void uv_loop_set_data(nint loop, void* data);
    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void* uv_loop_get_data(nint loop);
    

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int uv_async_send(nint async);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int uv_loop_close(nint loop);

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
    public static extern void uv_sleep(uint msec);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void uv_sem_post(ref uv_sem_t sem);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void uv_sem_wait(ref uv_sem_t sem);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void uv_sem_destroy(ref uv_sem_t sem);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int uv_mutex_init(out uv_mutex_t handle);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void uv_mutex_lock(ref uv_mutex_t handle);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void uv_mutex_unlock(ref uv_mutex_t handle);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void uv_mutex_destroy(ref uv_mutex_t handle);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int uv_replace_allocator(uv_alloc_cb alloc, uv_free_cb free);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int uv_cancel(uv_work_t* req);

    public delegate void uv_async_cb(nint handle);
    public delegate void uv_close_cb(nint handle);
    public delegate void uv_timer_cb(nint handle);
    public delegate void uv_thread_cb(nint arg);
    public delegate void uv_after_work_cb(uv_work_t* req, int status);
    public delegate void uv_work_cb(uv_work_t* req);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate nint uv_alloc_cb(nint size, nint align, nint zero_fill);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void uv_free_cb(nint ptr);

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
        public nint handle;

        public override string ToString() => $"[threadId 0x{handle:X}]";
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct uv_sem_t
    {
        public nint handle;
        public override string ToString() => $"[semId 0x{handle:X}]";
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct uv_work_t
    {
        public nint handle;
        public override string ToString() => $"[workId 0x{handle:X}]";
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct uv_mutex_t
    {
        private nint handle;
        public override string ToString() => $"[mutexId 0x{handle:X}]";
    }

    public struct uv_buf_t
    {
        private readonly IntPtr _field0;
        private readonly IntPtr _field1;

        public uv_buf_t(IntPtr memory, int len, bool IsWindows)
        {
            if (IsWindows)
            {
                _field0 = (IntPtr)len;
                _field1 = memory;
            }
            else
            {
                _field0 = memory;
                _field1 = (IntPtr)len;
            }
        }
    }

    public enum HandleType
    {
        Unknown = 0,
        ASYNC,
        CHECK,
        FS_EVENT,
        FS_POLL,
        HANDLE,
        IDLE,
        NAMED_PIPE,
        POLL,
        PREPARE,
        PROCESS,
        STREAM,
        TCP,
        TIMER,
        TTY,
        UDP,
        SIGNAL,
    }

    public enum RequestType
    {
        Unknown = 0,
        REQ,
        CONNECT,
        WRITE,
        SHUTDOWN,
        UDP_SEND,
        FS,
        WORK,
        GETADDRINFO,
        GETNAMEINFO,
    }
}
