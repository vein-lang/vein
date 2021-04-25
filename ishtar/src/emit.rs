use libc::c_void;
use linq::iter::Enumerable;

use std::{
    pin::Pin,
    fs::File,
    io::{BufReader, Read},
    collections::{HashMap}
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

pub struct TypeName {}
pub struct FieldName {}
pub struct IshtarModule
{
    pub name: *const str,
    pub version: *const str,
    pub deps: *const Vec<*const IshtarModule>,

    pub classes_table: *const Vec<* const IshtarClass>,
    pub strings_table: *mut HashMap<i32, *const str>,
    pub types_table: *mut HashMap<i32, *const TypeName>,
    pub fields_table: *mut HashMap<i32, *const FieldName>
}

impl IshtarModule
{
    pub fn new_1() -> Self
    {
        return Self {
            name: "",
            version: "",
            deps: &Vec::new() as *const Vec<*const IshtarModule>,
            classes_table: &Vec::new() as *const Vec<*const IshtarClass>,
            strings_table: &HashMap::new() as *const HashMap<i32, *const str> as *mut HashMap<i32, *const str>,
            types_table: &HashMap::new() as *const HashMap<i32, *const TypeName> as *mut HashMap<i32, *const TypeName>,
            fields_table: &HashMap::new() as *const HashMap<i32, *const FieldName>as *mut HashMap<i32, *const FieldName>,
        }
    }
    pub unsafe fn GetConstStrByIndex(self: &Self, index: &i32) -> Option<& *const str>
    {
        self.strings_table.as_ref().unwrap().get(&index)
    }
    pub unsafe fn GetTypeNameByIndex(self: &Self, index: &i32) -> Option<& *const TypeName>
    {
        self.types_table.as_ref().unwrap().get(&index)
    }
    pub unsafe fn GetFieldNameByIndex(self: &Self, index: &i32) -> Option<& *const FieldName>
    {
        self.fields_table.as_ref().unwrap().get(&index)
    }
}
pub struct IshtarClass {}
pub struct ConstStorage {}
pub struct IshtarAssembly
{
    pub module: *const IshtarModule,
    pub name: *const str,
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

