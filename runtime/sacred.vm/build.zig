const std = @import("std");

pub fn build(b: *std.Build) void {
    const target = b.standardTargetOptions(.{});
    const optimize = b.standardOptimizeOption(.{});

    const lib = b.addStaticLibrary(.{
        .name = "sacred.vm",
        .root_source_file = b.path("src/root.zig"),
        .target = target,
        .optimize = optimize,
    });

    const libuv_source_dir = "lib/libuv/src";
    const libuv_include_dir = "lib/libuv/include";
    const libuv = b.addStaticLibrary(.{});
    libuv.addIncludePath(libuv_include_dir);

    if (target.os.tag == .windows) {
        libuv.addCSourceFiles(&.{
            libuv_source_dir ++ "/fs-poll.c",
            libuv_source_dir ++ "/inet.c",
            libuv_source_dir ++ "/threadpool.c",
            libuv_source_dir ++ "/uv-common.c",
            libuv_source_dir ++ "/version.c",
            libuv_source_dir ++ "/win/async.c",
            libuv_source_dir ++ "/win/core.c",
            libuv_source_dir ++ "/win/dl.c",
            libuv_source_dir ++ "/win/error.c",
            libuv_source_dir ++ "/win/fs.c",
            libuv_source_dir ++ "/win/getaddrinfo.c",
            libuv_source_dir ++ "/win/getnameinfo.c",
            libuv_source_dir ++ "/win/handle.c",
            libuv_source_dir ++ "/win/loop-watcher.c",
            libuv_source_dir ++ "/win/pipe.c",
            libuv_source_dir ++ "/win/thread.c",
            libuv_source_dir ++ "/win/poll.c",
            libuv_source_dir ++ "/win/process.c",
            libuv_source_dir ++ "/win/signal.c",
            libuv_source_dir ++ "/win/stream.c",
            libuv_source_dir ++ "/win/tcp.c",
            libuv_source_dir ++ "/win/tty.c",
            libuv_source_dir ++ "/win/udp.c",
        }, &[_][]const u8{});
    } else {
        libuv.addCSourceFiles(&.{
            libuv_source_dir ++ "/fs-poll.c",
            libuv_source_dir ++ "/inet.c",
            libuv_source_dir ++ "/threadpool.c",
            libuv_source_dir ++ "/uv-common.c",
            libuv_source_dir ++ "/version.c",
            libuv_source_dir ++ "/async.c",
            libuv_source_dir ++ "/core.c",
            libuv_source_dir ++ "/fs.c",
            libuv_source_dir ++ "/getaddrinfo.c",
            libuv_source_dir ++ "/getnameinfo.c",
            libuv_source_dir ++ "/loop-watcher.c",
            libuv_source_dir ++ "/loop.c",
            libuv_source_dir ++ "/pipe.c",
            libuv_source_dir ++ "/poll.c",
            libuv_source_dir ++ "/process.c",
            libuv_source_dir ++ "/signal.c",
            libuv_source_dir ++ "/stream.c",
            libuv_source_dir ++ "/tcp.c",
            libuv_source_dir ++ "/timer.c",
            libuv_source_dir ++ "/tty.c",
            libuv_source_dir ++ "/udp.c",
            libuv_source_dir ++ "/unix/async.c",
            libuv_source_dir ++ "/unix/core.c",
            libuv_source_dir ++ "/unix/dl.c",
            libuv_source_dir ++ "/unix/fs.c",
            libuv_source_dir ++ "/unix/getaddrinfo.c",
            libuv_source_dir ++ "/unix/getnameinfo.c",
            libuv_source_dir ++ "/unix/loop.c",
            libuv_source_dir ++ "/unix/loop-watcher.c",
            libuv_source_dir ++ "/unix/pipe.c",
            libuv_source_dir ++ "/unix/poll.c",
            libuv_source_dir ++ "/unix/process.c",
            libuv_source_dir ++ "/unix/signal.c",
            libuv_source_dir ++ "/unix/stream.c",
            libuv_source_dir ++ "/unix/tcp.c",
            libuv_source_dir ++ "/unix/thread.c",
            libuv_source_dir ++ "/unix/timer.c",
            libuv_source_dir ++ "/unix/tty.c",
            libuv_source_dir ++ "/unix/udp.c",
        }, &[_][]const u8{
            "-D_LARGEFILE_SOURCE",
            "-D_FILE_OFFSET_BITS=64",
            "-pthread",
        });
    }

    b.installArtifact(lib);

    const exe = b.addExecutable(.{
        .name = "sacred.vm",
        .root_source_file = b.path("src/main.zig"),
        .target = target,
        .optimize = optimize,
    });

    exe.linkLibrary(libuv);

    b.installArtifact(exe);

    const run_cmd = b.addRunArtifact(exe);

    run_cmd.step.dependOn(b.getInstallStep());

    if (b.args) |args| {
        run_cmd.addArgs(args);
    }

    const run_step = b.step("run", "Run the app");
    run_step.dependOn(&run_cmd.step);
}
