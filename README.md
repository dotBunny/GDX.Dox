# GDX.Dox

This repository contains the tooling used to generate the documentation for [GDX](https://github.com/dotBunny/GDX).

Dox a [.NET 6](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) console application which adds context specific functionality to the [DocFX](https://dotnet.github.io/docfx/) generation process. It explictly depends on an assumed file structure found in the [GDX](https://github.com/dotBunny/GDX) package.

## Generate

The `--generate` argument will execute the logic required for parsing the API and generating the static content to be served for documentation, by default it will spin up a hosting process as well.

> If no arguements are provided by default Dox will assume a `--generate` arguement has been passed.

## Deploy

The `--deploy` argument is used to create and push a GIT commit to a remote repository of the udpated documentation.
