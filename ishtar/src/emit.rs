use libc::c_void;

use std::{
    fs::File,
    io::{BufReader, Read}
};
use elf_rs::*;

#[repr(u32)]
pub enum CallContext
{
    NATIVE_CALL = 1,
    THIS_CALL = 2,
    STATIC_CALL = 3,
    BACKWARD_CALL = 4
}


pub struct IshtarModule {}
pub struct IshtarClass {}

pub struct IshtarAssembly
{

}

pub struct WaveMethod
{}
pub struct StackVal
{}
pub struct CallFrameException
{}
pub struct CallFrame
{
    pub parent: *mut CallFrame,
    pub method: *mut WaveMethod,
    pub return_value: *mut StackVal,
    pub _this_: *mut c_void,
    pub args: *mut StackVal,
    pub stack: *mut StackVal,
    pub level: u32,
    pub exception: *mut CallFrameException
}

// ==================== impl =====

