namespace ishtar.runtime.io;

using gc;
using libuv;
using static libuv.LibUV;

public unsafe struct IshtarFile
{
    public static bool exist(string path)
    {
        return File.Exists(path); // temporary
        //fixed (char* p = path)
        //{
        //    var loop = uv_default_loop();
        //    uv_fs_t req;
        //    var statResult = uv_fs_stat(loop, &req, p, Cb);

        //    unsafe void Cb(uv_fs_t* uvFsT)
        //    {
        //    }

        //    if (statResult < 0)
        //        return false;
        //    return true;
        //}
    }

    public static SlicedString readAllFile(string path)
    {
        var str = File.ReadAllText(path);
        var mem = str.AsMemory();

        return new SlicedString((char*)mem.Pin().Pointer, (uint)mem.Length);
        // todo temporary
        var loop = uv_default_loop();

        uv_fs_t req;

        var statResult = uv_fs_stat(loop, &req, null, null);

        if (statResult < 0)
            throw new Exception($"Failed to stat file: {statResult}");

        var stat = Unsafe.AsRef<uv_stat_t>((void*)req.result);

        ulong fileSize = stat.st_size;

        if (fileSize > int.MaxValue)
            throw new NotSupportedException();
        
        var file = uv_fs_open(loop, &req, path, 0, 0, null);

        if (file < 0)
            throw new Exception($"Failed to open file: {(UV_ERR)file}");

        var buffers = stackalloc uv_buf_t[1];

        char* buffer = IshtarGC.AllocateImmortal<char>((int)fileSize, null);

        buffers[0] = new uv_buf_t
        {
            basePtr = buffer,
            len = (IntPtr)fileSize
        };

        var readResult = uv_fs_read(loop, &req, file, buffers, 1, 0, null);
        if (readResult < 0)
        {
            uv_fs_close(loop, &req, file, null);
            throw new Exception($"Failed to read file: {readResult}");
        }

        var nativeString = new SlicedString(buffer, (uint)readResult);

        var closeResult = uv_fs_close(loop, &req, file, null);
        if (closeResult < 0)
            throw new Exception($"Failed to close file: {closeResult}");

        return nativeString;
    }
}
