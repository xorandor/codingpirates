# Project overview
A game project used for teaching a student group of 10-15 year old about coding. The project is intended to evolve over the course with the group, 
where the goal is to elolve the game with new concepts that are interesting from a coding learning perspective, as well as just being fun and inspiring.

# Tech stack
- Language/runtime: C# and dotnet 10
- Frameworks: raylib for everything gaming related, due to its simplicity. dotnet BCL for networking and general stuff

# Project structure
The project must be simple to work with. With a focus on enabling the student group to copy/paste single files into their own local copy (without pulling with git).
Therefore the project is structured around the concept of individual components that can be copy/pasted as single files. These components must always
be expressed as classes, following a basic object oriented structure

The project has three concepts:
- Core folder: Contains the basic framework that supports the components. Components and composition are allowed to reference this part of the project.
- Components folder: Contains the individual components. A component must always be a single file without dependencies on other components. Only composition is allowed to refer this part of the project.
- program.cs: Considered the composition of the project. This is where students compose the game into their own construction. The composition brings everything together, and hence is allowed to reference everything else.

# Development setup
The project can be run immediately by executing the command `dotnet run`` inside this folder

# Conventions & Preferences
- When working with positioning, always use vectors (Vector2 from raylib). When working with directions, always use vectors with length 1. If distance/speed is required, is must be a seperate float value.
- In general always use raylib method overloads that accepts vector arguments instead of float values for x and y components