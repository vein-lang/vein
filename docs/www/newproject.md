# Generating a new project

Let’s write a small application with our new Vein development environment.
To start, we’ll use rune to make a new project for us. 
In your terminal of choice run:

```bash [console]
rune new
```


This will generate a new directory with the following files:

```
name-of-your-project
|- name-of-your-project.vproj
|- src
  |- main.vein
```


`name-of-your-project.vproj` is the project file for Vein. 
It’s where you keep metadata for your project, as well as dependencies.         
`src/main.vein` is where we’ll write our application code.          



### Adding dependencies

Let’s add a dependency to our application.      
You can find all sorts of libraries on [gallery](https://gallery.vein-lang.org), the package registry for Vein.    
In Vein, we often refer to packages as `“shard”`


In this project, we’ll use a shard called `cow`.            
In our *.vproj file we’ll add this information (that we got from the gallery page):

```yaml
packages:
- cow@1.0.0
```

We can also do this by running      

```bash
rune add cow
```

Now we can run:

```bash
rune build
```

## Fast start

```bash [console]
cd ~/
mkdir cool_project
cd cool_project
rune new
rune run
code . 
```