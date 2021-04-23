use std::{collections::HashMap, pin::Pin};

use crate::emit::
{
    IshtarAssembly, 
    IshtarClass, 
    IshtarModule
};

pub struct IshtarContext
{
    pub modules_cache: Pin<Box<HashMap<String, IshtarModule>>>,
    pub classes_cache: Pin<Box<HashMap<String, IshtarClass>>>,
    pub loaded_assemlies: Pin<Box<HashMap<String, IshtarAssembly>>>,
}