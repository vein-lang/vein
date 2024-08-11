namespace ishtar.libuv;

using System.Runtime.InteropServices;
using static ishtar.libuv.LibUV;

public static unsafe class LibUV
{
    private const string LIBNAME = "libuv";

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int uv_thread_create(out uv_thread_t tid, uv_thread_cb entry, IntPtr arg);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
    public static extern int uv_thread_join([In]in uv_thread_t tid);
    
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

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void uv_loop_set_data(nint loop, void* data);
    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void* uv_loop_get_data(nint loop);
    

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern UV_ERR uv_async_send(nint async);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern UV_ERR uv_loop_close(nint loop);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void uv_close(nint handle, uv_close_cb cb);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong uv_thread_self();

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern nint uv_default_loop();
    
    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern UV_ERR uv_timer_init(nint loop, nint handle);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern UV_ERR uv_timer_start(nint handle, uv_timer_cb cb, ulong timeout, ulong repeat);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void uv_timer_stop(nint handle);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern UV_ERR uv_queue_work(nint loop, ref uv_work_t req, uv_work_cb work_cb, uv_after_work_cb after_work_cb);
    
    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern UV_ERR uv_sem_init(out uv_sem_t sem, int value);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void uv_sleep(uint msec);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void uv_sem_post(ref uv_sem_t sem);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void uv_sem_wait(ref uv_sem_t sem);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void uv_sem_destroy(ref uv_sem_t sem);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern UV_ERR uv_mutex_init(out uv_mutex_t handle);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void uv_mutex_lock(ref uv_mutex_t handle);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void uv_mutex_unlock(ref uv_mutex_t handle);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void uv_mutex_destroy(ref uv_mutex_t handle);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern UV_ERR uv_cancel(uv_work_t* req);
    
    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern UV_ERR uv_tcp_init(nint loop, uv_tcp_t* handle);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern UV_ERR uv_tcp_init_ex(nint loop, ref uv_tcp_t handle, uint flags);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern UV_ERR uv_tcp_open(ref uv_tcp_t handle, uv_os_sock_t sock);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern UV_ERR uv_tcp_nodelay(ref uv_tcp_t handle, int enable);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern UV_ERR uv_tcp_keepalive(ref uv_tcp_t handle, int enable, uint delay);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern UV_ERR uv_tcp_simultaneous_accepts(ref uv_tcp_t handle, int enable);
    
    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern UV_ERR uv_tcp_bind(uv_tcp_t* handle, ref sockaddr_in addr, uint flags);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern UV_ERR uv_listen(uv_tcp_t* stream, int backlog, delegate*<uv_tcp_t*, int, void> cb);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern UV_ERR uv_read_start(ref uv_stream_t stream, uv_alloc_cb alloc_cb, uv_read_cb read_cb);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern UV_ERR uv_accept(IntPtr server, IntPtr client);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern uv_buf_t uv_buf_init(IntPtr basePtr, uint len);

    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern UV_ERR uv_ip4_addr(string ip, int port, out sockaddr_in addr);


    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int uv_replace_allocator(
        uv_malloc_func malloc_func,
        uv_realloc_func realloc_func,
        uv_calloc_func calloc_func,
        uv_free_func free_func
    );

    public delegate IntPtr uv_malloc_func(UIntPtr size);
    public delegate IntPtr uv_realloc_func(IntPtr ptr, UIntPtr size);
    public delegate IntPtr uv_calloc_func(UIntPtr count, UIntPtr size);
    public delegate void uv_free_func(IntPtr ptr);


    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void uv_fs_req_cleanup(uv_fs_t* req);
    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int uv_fs_open(IntPtr loop, uv_fs_t* req, string path, int flags, int mode, uv_fs_cb cb);
    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern UV_ERR uv_fs_read(IntPtr loop, uv_fs_t* req, int file, uv_buf_t* buffers, uint number_buffers, long offset, uv_fs_cb cb);
    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern UV_ERR uv_fs_close(IntPtr loop, uv_fs_t* req, int file, uv_fs_cb cb);
    [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern UV_ERR uv_fs_stat(IntPtr loop, uv_fs_t* req, char* path, uv_fs_cb cb);


    public delegate void uv_connection_cb(IntPtr server, int status);
    public delegate void uv_conn_alloc_cb(IntPtr handle, ulong suggested_size, out uv_buf_t buf);
    public delegate void uv_read_cb(IntPtr stream, nint nread, ref uv_buf_t buf);
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
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void uv_fs_cb(uv_fs_t* req);
    

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
    public unsafe struct uv_fs_t
    {
        public IntPtr data;
        public IntPtr loop;
        public int fs_type;
        public IntPtr result;
        public IntPtr ptr;
        public IntPtr path;
        public uv_buf_t buf;
        public IntPtr cb;
        public IntPtr fs_req_cleanup;
        public IntPtr syscall;
        public IntPtr args;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct uv_buf_t
    {
        public IntPtr basePtr;
        public IntPtr len;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct sockaddr_in
    {
        public short sin_family;
        public ushort sin_port;
        public uint sin_addr;
        public ulong sin_zero;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct uv_thread_t
    {
        public uv_handle_t handle;

        public override string ToString() => $"[threadId {handle}]";
    }

    public enum UvHandleType
    {
        UNKNOWN_HANDLE = 0,
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
        FILE
    }

    /*  XX(ASYNC, async)                                                            \
       XX(CHECK, check)                                                            \
       XX(FS_EVENT, fs_event)                                                      \
       XX(FS_POLL, fs_poll)                                                        \
       XX(HANDLE, handle)                                                          \
       XX(IDLE, idle)                                                              \
       XX(NAMED_PIPE, pipe)                                                        \
       XX(POLL, poll)                                                              \
       XX(PREPARE, prepare)                                                        \
       XX(PROCESS, process)                                                        \
       XX(STREAM, stream)                                                          \
       XX(TCP, tcp)                                                                \
       XX(TIMER, timer)                                                            \
       XX(TTY, tty)                                                                \
       XX(UDP, udp)                                                                \
       XX(SIGNAL, signal) */

    [StructLayout(LayoutKind.Sequential)]
    public struct uv_tcp_t
    {
        public void* data;
        public void* loop;
        public UvHandleType uv_handle_type;
        public uv_stream_t stream;

        public override string ToString() => $"[uv_tcp_t {stream}]";
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct uv_stream_t
    {
        public uv_handle_t handle;
        public override string ToString() => $"[uv_stream_t {handle}]";
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct uv_handle_t
    {
        public nint handle;
        public override string ToString() => $"[uv_handle_t 0x{handle:X}]";
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct uv_sem_t
    {
        public uv_handle_t handle;
        public override string ToString() => $"[semId {handle}]";
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct uv_work_t
    {
        public nint handle;
        public override string ToString() => $"[workId 0x{handle:X}]";
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct uv_os_sock_t
    {
        public nint Sock;

        public override string ToString() => $"[workId 0x{Sock:X}]";
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct uv_mutex_t
    {
        private nint handle;
        public override string ToString() => $"[mutexId 0x{handle:X}]";
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct uv_stat_t
    {
        public ulong st_dev;
        public ulong st_mode;
        public ulong st_nlink;
        public ulong st_uid;
        public ulong st_gid;
        public ulong st_rdev;
        public ulong st_ino;
        public ulong st_size;
        public ulong st_blksize;
        public ulong st_blocks;
        public ulong st_flags;
        public ulong st_gen;
        public ulong st_atim;
        public ulong st_mtim;
        public ulong st_ctim;
        public ulong st_birthtim;
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

public enum UV_ERR
{
    OK = 0,
    UV__EOF = -4095,
    UV__UNKNOWN = -4094,

    UV__EAI_ADDRFAMILY = -3000,
    UV__EAI_AGAIN = -3001,
    UV__EAI_BADFLAGS = -3002,
    UV__EAI_CANCELED = -3003,
    UV__EAI_FAIL = -3004,
    UV__EAI_FAMILY = -3005,
    UV__EAI_MEMORY = -3006,
    UV__EAI_NODATA = -3007,
    UV__EAI_NONAME = -3008,
    UV__EAI_OVERFLOW = -3009,
    UV__EAI_SERVICE = -3010,
    UV__EAI_SOCKTYPE = -3011,
    UV__EAI_BADHINTS = -3013,
    UV__EAI_PROTOCOL = -3014,

    UV__E2BIG = -4093,
    UV__EACCES = -4092,
    UV__EADDRINUSE = -4091,
    UV__EADDRNOTAVAIL = -4090,
    UV__EAFNOSUPPORT = -4089,
    UV__EAGAIN = -4088,
    UV__EALREADY = -4084,
    UV__EBADF = -4083,
    UV__EBUSY = -4082,
    UV__ECANCELED = -4081,
    UV__ECHARSET = -4080,
    UV__ECONNABORTED = -4079,
    UV__ECONNREFUSED = -4078,
    UV__ECONNRESET = -4077,
    UV__EDESTADDRREQ = -4076,
    UV__EEXIST = -4075,
    UV__EFAULT = -4074,
    UV__EHOSTUNREACH = -4073,
    UV__EINTR = -4072,
    UV__EINVAL = -4071,
    UV__EIO = -4070,
    UV__EISCONN = -4069,
    UV__EISDIR = -4068,
    UV__ELOOP = -4067,
    UV__EMFILE = -4066,
    UV__EMSGSIZE = -4065,
    UV__ENAMETOOLONG = -4064,
    UV__ENETDOWN = -4063,
    UV__ENETUNREACH = -4062,
    UV__ENFILE = -4061,
    UV__ENOBUFS = -4060,
    UV__ENODEV = -4059,
    UV__ENOENT = -4058,
    UV__ENOMEM = -4057,
    UV__ENONET = -4056,
    UV__ENOSPC = -4055,
    UV__ENOSYS = -4054,
    UV__ENOTCONN = -4053,
    UV__ENOTDIR = -4052,
    UV__ENOTEMPTY = -4051,
    UV__ENOTSOCK = -4050,
    UV__ENOTSUP = -4049,
    UV__EPERM = -4048,
    UV__EPIPE = -4047,
    UV__EPROTO = -4046,
    UV__EPROTONOSUPPORT = -4045,
    UV__EPROTOTYPE = -4044,
    UV__EROFS = -4043,
    UV__ESHUTDOWN = -4042,
    UV__ESPIPE = -4041,
    UV__ESRCH = -4040,
    UV__ETIMEDOUT = -4039,
    UV__ETXTBSY = -4038,
    UV__EXDEV = -4037,
    UV__EFBIG = -4036,
    UV__ENOPROTOOPT = -4035,
    UV__ERANGE = -4034,
    UV__ENXIO = -4033,
    UV__EMLINK = -4032,
    UV__EHOSTDOWN = -4031,
    UV__EREMOTEIO = -4030,
    UV__ENOTTY = -4029,
    UV__EFTYPE = -4028,
    UV__EILSEQ = -4027,
    UV__EOVERFLOW = -4026,
    UV__ESOCKTNOSUPPORT = -4025,
    UV__ENODATA = -4024,
    UV__EUNATCH = -4023
}
