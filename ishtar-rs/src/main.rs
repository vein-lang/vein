#![allow(non_camel_case_types)]
#![allow(non_upper_case_globals)]
#![allow(non_snake_case)]
#![allow(unused_variables)]

extern crate libc;
extern crate elf_rs;
extern crate binary_reader;
extern crate foreach;

pub mod emit;
pub mod vm;


use std::{
    collections::{HashMap}, 
    env, 
    process, 
    path::{Path}, 
    fs::File,
    io::{BufReader, Read}
};
use std::str;
use ctor::{dtor};
use elf_rs::*;
use foreach::*;
use linq::linq;
use linq::iter::Enumerable;

use emit::{CallFrame, IshtarAssembly, IshtarClass, IshtarModule};
use vm::IshtarContext;
use binary_reader::BinaryReader;

#[dtor]
unsafe fn shutdown() {
    libc::puts("vm down!\n\0".as_ptr() as *const i8);
}

trait IshtarReader {
    fn read_ishtar_string(&self) -> &str { "" } 
    fn validate_ishtar_magic(&self) -> () { }
}

impl IshtarReader for BinaryReader
{
    fn read_ishtar_string(&self) -> &str 
    {
        self.validate_ishtar_magic();
        let size: usize = self.read_i32().unwrap() as usize;
        let bytes = self.read(size).unwrap();
        str::from_utf8(&bytes).unwrap()
    } 
    fn validate_ishtar_magic(&self) -> ()
    {
        match self.read_u8().unwrap() {
            0xFF => {}
            x => panic!()
        }; 
        match self.read_u8().unwrap() {
            0xFF => {}
            x => panic!()
        }; 
    }
}


impl IshtarAssembly
{
    #[cfg(debug_assertions)]
    pub unsafe fn open(f: File) -> Option<IshtarAssembly>
    {
        let mut reader = BufReader::new(f);
        let mut elf_buf = Vec::<u8>::new();
        reader.read_to_end(&mut elf_buf).unwrap();

        let elf = Elf::from_bytes(&elf_buf).unwrap();
        

        let z = IshtarAssembly::lookup_section(&elf, ".il_code").unwrap();
        let reader = &BinaryReader::from_u8(z.segment()) 
            as *const BinaryReader 
            as *mut BinaryReader;
        
        IshtarAssembly::module_reader(reader);

        return None;
    }

    fn lookup_section<'a>(elf: &'a Elf, name: &str) -> Option<SectionHeader<'a, u32>>
    {
        if let Elf::Elf32(e) = elf {
            return e.lookup_section(name);
        }
        return None;
    }

    unsafe fn module_reader(raw: *mut BinaryReader) -> *mut IshtarModule
    {
        let module: *mut IshtarModule = IshtarModule::new_1() as *const _ as *mut _;
        let reader = raw.as_mut().unwrap();
        let idx = reader.read_i32().unwrap();
        let vdx = reader.read_i32().unwrap();

        let str_table_len = 0..reader.read_i32().unwrap();

        for n in 1..reader.read_i32().unwrap()
        {
            let key = reader.read_i32().unwrap();
            let str = reader.read_ishtar_string();
            module.as_mut().unwrap().strings_table.insert(key, str as *const str);
        }

        &module
    }
}



fn main()
{
    unsafe 
    {
        let vm_ctx: IshtarContext = IshtarContext {
            modules_cache: &HashMap::new() as *const HashMap<*const str, *const IshtarModule>,
            classes_cache: &HashMap::new() as *const HashMap<*const str, *const IshtarClass>,
            loaded_assemlies: &HashMap::new() as *const HashMap<*const str, *const IshtarAssembly>,
        };
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
    
        let w: *const IshtarContext = &vm_ctx;
        init_default(w);
        init_strings_phase_1(w);
        init_types(w);
        init_tables(w);
        init_strings_phase_2(w);
    }
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
