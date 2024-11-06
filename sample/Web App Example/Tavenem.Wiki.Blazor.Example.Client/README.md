Tavenem.Wiki.Blazor.Example
==

This project is a bare-bones example with the minimum required code for a `NeverFoundry.Wiki.Blazor`
wiki.

The sample uses a project reference to Tavenem.Wiki.Blazor.Client (i.e. not a package references to
the NuGet release). The easiest way to get it up and running is to clone the entire repo to your own
system, then run the example server project.

## WARNING
This sample is **not** suitable for use in a production environment. It's using an
in-memory database and mocked user identity system. ***DO NOT*** copy this code as-is and put it
into production. It is intended only for you to quickly and easily experience the capabilities and
the look-and-feel of the main package's defaults in a live sandbox.

A production implementation should override the various properties as described in the README of
this project and [Tavenem.Wiki](https://github.com/Tavenem/Wiki) with production-ready values and
services.