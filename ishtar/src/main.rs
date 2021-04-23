#![allow(non_camel_case_types)]
#![allow(non_upper_case_globals)]
#![allow(non_snake_case)]
#![allow(unused_variables)]

extern crate libc;
extern crate elf_rs;
extern crate binary_reader;

pub mod emit;
pub mod vm;


use std::{
    collections::{HashMap}, 
    pin::Pin, 
    env, 
    process, 
    path::{Path}, 
    fs::File,
    io::{BufReader, Read}
};
use ctor::{ctor, dtor};
use elf_rs::*;

use linq::linq;
use linq::iter::Enumerable;

use emit::{CallFrame, IshtarAssembly};
use vm::IshtarContext;
use binary_reader::BinaryReader;


#[ctor]
static vm_ctx: Pin<Box<IshtarContext>> = {
    let ctx: IshtarContext = IshtarContext {
        modules_cache: Pin::new(Box::from(HashMap::new())),
        classes_cache: Pin::new(Box::from(HashMap::new())),
        loaded_assemlies: Pin::new(Box::from(HashMap::new())),
    };
    Pin::new(Box::from(ctx))
};
 
#[dtor]
unsafe fn shutdown() {
    libc::puts("vm down!\n\0".as_ptr() as *const i8);
}




impl IshtarAssembly
{
    #[cfg(debug_assertions)]
    pub fn open(f: File) -> Option<IshtarAssembly>
    {
        let mut reader = BufReader::new(f);
        let mut elf_buf = Vec::<u8>::new();
        reader.read_to_end(&mut elf_buf).unwrap();

        let elf = Elf::from_bytes(&elf_buf).unwrap();
        

        let z = IshtarAssembly::lookup_section(&elf, ".il_code").unwrap();
        let reader = BinaryReader::from_u8(z.segment());
        return None;
    }

    fn lookup_section<'a>(elf: &'a Elf, name: &str) -> Option<SectionHeader<'a, u32>>
    {
        if let Elf::Elf32(e) = elf {
            return e.lookup_section(name);
        }
        return None;
    }
}
fn main() -> ()
{
    println!("Ishtar VM has starting initialization...");
    let file = match env::args().skip(1).next() {
        None => process::exit(-1),
        Some(x) => x
    };
    match Path::new(&file).exists() {
        false => process::exit(-2),
        true => println!("Load application {}...", &file)
    };
    let stream = match File::open(&file) {
        Err(_) => process::exit(-3),
        Ok(x) => x
    };
   
    let asm = match IshtarAssembly::open(stream) {
        None => process::exit(-4),
        Some(x) => x
    };

    let w: *const IshtarContext = vm_ctx.as_ref().get_ref();
    init_default(w);
    init_strings_phase_1(w);
    init_types(w);
    init_tables(w);
    init_strings_phase_2(w);
}

fn init_default(ctx: *const IshtarContext)
{
}
fn init_strings_phase_1(ctx: *const IshtarContext)
{}
fn init_types(ctx: *const IshtarContext)
{}
fn init_tables(ctx: *const IshtarContext)
{}
fn init_strings_phase_2(ctx: *const IshtarContext)
{}



pub fn CallFrame__fill_stacktrace(frame: *mut CallFrame)
{
}
