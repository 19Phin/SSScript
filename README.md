in bin\Debug\net8.0 is the exe (you can put it wherever you want) put your creation in there, code the program in program.txt, and run the exe. it will wipe the nodegraph in the process. it follows semi C++ format, but is more of an almalgamation of many languages
it currently only takes a main method
any variables defined outside the main method appear as actual variables in game, the method arguments are the module input variables, you define a method as "def" and not any type as return type doesnt matter
no for/while loops, there is if/else statements 
you can return any number of variables, they go into the module outputs
do not return in an if statement
all nodes spawn at center of the screen with a few exceptions
it cannot handle any peice nodes so marking a local var as "public" without an assignment moves it over a bit so you can replace it
global variable set nodes are moved up incase you need to interrupt their execution paths
and any variable that is unused is moved over so you can take outputs
error handling is crap, just fails to update the creation
umm
method calls consist of
    "V3.cross",
    "V3.dot",
    "V3.magnitude",
    "V3.normalize",
    "V3.project",
    "V3.project_on_plane",
    "V3.up",
    "V3.camera_pivot_horizontal",
    "V3.camera_pivot_vertical",
    "V3.camera_tilt_horizontal",
    "V3.camera_tilt_vertical",
    "V3.angle",
    "Math.sign",
    "Math.delta_Time",
    "Math.sin",
    "Math.cos",
    "Math.tan",
    "Math.clamp",
    "Math.clamp_01",
    "Math.float_lerp",
    "Math.abs",
    "SS.toggleNode",
    "SS.trigger",
    "Math.sqrt",
    "Math.acos",
    "Math.asin",
    "Math.atan",
all input nodes are just their names with underscores for spaces
dont worry about capitalization
return has to be in parenthesees
umm
types arent really followed, and vars can be set to anything
i think thats about it
