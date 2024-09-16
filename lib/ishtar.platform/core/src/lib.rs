use std::ffi::CString;


#[cfg(target_os = "windows")]
mod platform {
    use windows::Win32::System::Threading::{GetCurrentThread, GetCurrentThreadStackLimits, SetThreadDescription};
    use windows::core::PWSTR;
    use std::ffi::OsStr;
    use std::os::windows::ffi::OsStrExt;
    pub struct StackInfo {
        pub top: usize,
        pub bottom: usize,
    }
    pub fn get_thread_stack() -> StackInfo {
        unsafe {
            let mut stack_limit: usize = 0;
            let mut stack_base: usize = 0;
            GetCurrentThreadStackLimits(&mut stack_limit as *mut usize, &mut stack_base as *mut usize);
            StackInfo {
                top: stack_base,
                bottom: stack_limit,
            }
        }
    }

    pub fn set_thread_name(name: &str) {
        unsafe {
            let thread_handle = GetCurrentThread();
            let wide_name: Vec<u16> = OsStr::new(name).encode_wide().chain(Some(0)).collect();
            SetThreadDescription(thread_handle, PWSTR(wide_name.as_ptr().cast_mut())).expect("SetThreadDescription");
        }
    }
}
#[cfg(unix)]
mod platform {
    use libc::{pthread_getattr_np, pthread_self, pthread_attr_getstack, pthread_t};
    use std::mem;
    use std::ffi::CString;
    pub struct StackInfo {
        pub top: usize,
        pub bottom: usize,
    }

    pub fn get_thread_stack() -> StackInfo {
        unsafe {
            let mut attr = mem::zeroed();
            pthread_getattr_np(pthread_self(), &mut attr);
            let mut stack_addr: *mut libc::c_void = std::ptr::null_mut();
            let mut stack_size: usize = 0;
            pthread_attr_getstack(&attr, &mut stack_addr, &mut stack_size);
            StackInfo {
                top: (stack_addr as usize) + stack_size,
                bottom: stack_addr as usize,
            }
        }
    }

    pub fn set_thread_name(name: &str) {
        let c_name = CString::new(name).expect("Invalid thread name");
        unsafe {
            libc::pthread_setname_np(pthread_self(), c_name.as_ptr());
        }
    }
}