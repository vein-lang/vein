use std::{collections::HashMap};

use crate::emit::
{
    IshtarAssembly, 
    IshtarClass, 
    IshtarModule
};

pub struct IshtarContext
{
    pub modules_cache: *const HashMap<*const str, *const IshtarModule>,
    pub classes_cache: *const HashMap<*const str, *const IshtarClass>,
    pub loaded_assemlies: *const HashMap<*const str, *const IshtarAssembly>,
}