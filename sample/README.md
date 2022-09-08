Tavenem.Wiki.Blazor.Sample
==

This project is a bare-bones example with the minimum required code for a `NeverFoundry.Wiki.Blazor`
wiki.

The sample uses project references to Tavenem.Wiki.Blazor.Client and Tavenem.Wiki.Blazor.Server (i.e. not package references to the NuGet
releases). The easiest way to get it up and running is to clone the entire repo to your own system,
then run the sample project.

## WARNING
This sample is **not** suitable for use in a production environment. Seriously. It's using an
in-memory database and mocked user identity system. ***DO NOT*** copy this code as-is and put it
into production. It is intended only for you to quickly and easily experience the capabilities and
the look-and-feel of the main package's defaults in a live sandbox.

A production implementation should override the various properties as described in the README of
this project, [Tavenem.Wiki](https://github.com/Tavenem/Wiki), and
[Tavenem.Wiki.Web](https://github.com/Tavenem/Wiki.Web) with production-ready values and services.