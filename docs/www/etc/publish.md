---
description: Publishing on gallery
---


# Publishing on gallery


When you're ready to share your library, you can publish it on [gallery](https://gallery.vein-lang.org)!         
Publishing a shard package involves uploading a particular version to be hosted on [gallery](https://gallery.vein-lang.org).            

Be cautious when publishing a shard package, as it is a permanent action.       
The version cannot be altered once published, and the code cannot be removed.         
However, you can publish as many versions as you like.       
Also shard package can be marked as depracated and not recommended to use.      



## Before your first publish

To get started, you’ll need an account on [gallery](https://gallery.vein-lang.org) to get an API token.         
Go to the home page and log in using your GitHub account.         
After that, create an API token and be sure to copy it.     


Then run `config set` command for save api token.
```bash
$ rune config set registry:api:token "YourApiToken"
```

This command will inform registry of your API token and store it locally in your ~/.vein/vcfg        
Note that this token is a secret and should not be shared with anyone else.     
If it leaks for any reason, you should revoke it immediately.      

:::tip
~/.vein/vcfg has encrypted by device token
:::



## Before publishing a new shard


Keep in mind that shard names on [gallery](https://gallery.vein-lang.org) are allocated on a first-come-first-serve basis.        
Once a shard name is taken, it cannot be used for another shard.        

:::tip
In the future, it is also planned to expand the functionality to create scoped packages for organizations
`@thebestcompany/shardname` e.g
:::


Check out the metadata you can specify in project.vproj to ensure your shard can be discovered more easily!     
Before publishing, make sure you have filled out the following fields:  

- license
- description
- authors
- repository

It would also be a good idea to include some keywords and categories, though they are not required.     

:::tip
also dont forget mark project as packable

```yaml
packable: true
```
:::




## Uploading the shard

When you are ready to publish, use the `rune publish` command to upload to crates.io:

```bash
$ rune publish
```
And that’s it, you’ve now published your first shard!