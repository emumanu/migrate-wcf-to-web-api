MigrateWcfToWebApi console app

Description:
converts WCF (and ASMX) endpoints to ASP.NET Web API endpoints. 
specifically auto-generates: 
- service api controller classes with route action methods internally calling wcf service provider methods
- client classes that make http client requests to service api endpoints 

Usage:
MigrateWcfToWebApi -wcfServiceDir [-serviceOutputDir] -wcfClientDir [-clientOutputDir] [-?]

Args:
[-wcfServiceDir]: directory of wcf service source files (ex. *.svc.cs, *.asmx.cs)

[-serviceOutputDir]: output directory for code generated web api controller files (ex. *Controller.cs). optional. if not specified then current directory used.

[-wcfClientDir]: directory of wcf client source files 

[-clientOutputDir]: output directory for code generated client files. optional. if not specified then current directory used.

[-?]: displays this usage
