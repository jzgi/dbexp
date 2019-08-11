# Welcome to WebReady  

WebReady is an extensible webapp framework that exposes well-designed database objects directly as RESTful web services.
It helps building performant, seure, transaction-oriented cloud applications. 

WebReady is based on the high-performance [Kestral HTTP server](https://github.com/aspnet/AspNetCore/tree/master/src/Servers/Kestrel) that is built in ASP.NET Core, as well as the [Npgsql data access library](https://www.npgsql.org/).

The best practics of database design are strictly followed:
* views -- database views are exposed as folders that support GET, POST, PUT and DELETE methods acccording to whether a view is updatable
* functions -- database functions are exposed as executable object that support GET, or POST, or both. A function has a number of parameters, that are naturally mapped to HTTP request parameters.
* procedures -- database stored procedures are to be handlers for async messaging
* role-based grants and access control

In addition, WebReady is able to
* Propagate web-tier context into SQL session
* Give detailed online API documentation on-the-fly
* Add custom action handling classes and methods 


<pre>
dotnet add package WebReady --version 1.0.0
</pre>
