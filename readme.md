Sitecore 7 Elasticsearch Provider
=================================
 
Usage
-----
* Clone or Fork from Bitbucket
* Compile
	* You will likely need to manually add references to Sitecore.Kernel, Sitecore.ContentSearch, Sitecore.ContentSearch.Linq
* Copy or reference the compiled Sitecore.ContentSearch.ElasticSearchProvider.dll and /App_Config/Include/Sitecore.ContentSearch.ElasticSearch.config files to your solution
	* Also copy/reference Nest.dll and Newtonsoft.Json.dll from the project /bin folder
	* You will need to overwrite the existing Newtonsoft.Json.dll file in the Sitecore /bin folder
		* There is probably a better way to handle this using assembly redirection, but I didn't explore that option
* Disable any other Sitecore search provider by deleting or renaming their respective .config files in the /App_Config/Include directory
	* Be sure not to disable or remove the /App_Config/Include/Sitecore.ContentSearch.config file
* Search away!

Known Issues
------------
A word of caution: as of this writing (07/26/2013), there are still some deal-breaking bugs to resolve. However, the majority of the search provider functionality is in place and operational (enough for you to test and play around with). This provider is still very much in beta form. Because challenging.

See the Issues page for current known issues and bugs:
https://bitbucket.org/aweber1/sitecore-elasticsearch-provider/issues?status=new&status=open

Dependencies
------------
* NEST (Elasticsearch .NET client) : https://github.com/Mpdreamz/NEST
	* note: NEST requires Newtonsoft.Json
* Sitecore 7

What is Elasticsearch?
----------------------
http://www.elasticsearch.org/

ElasticSearch is a distributed RESTful search engine built for the cloud. Features include:

* Distributed and Highly Available Search Engine.
	* Each index is fully sharded with a configurable number of shards.
	* Each shard can have one or more replicas.
	* Read / Search operations performed on either one of the replica shard.
* Multi Tenant with Multi Types.
	* Support for more than one index.
	* Support for more than one type per index.
	* Index level configuration (number of shards, index storage, …).
* Various set of APIs
	* HTTP RESTful API
	* Native Java API.
	* All APIs perform automatic node operation rerouting.
* Document oriented
	* No need for upfront schema definition.
	* Schema can be defined per type for customization of the indexing process.
* Reliable, Asynchronous Write Behind for long term persistency.
* (Near) Real Time Search.
* Built on top of Lucene
	* Each shard is a fully functional Lucene index
	* All the power of Lucene easily exposed through simple configuration / plugins.
* Per operation consistency
	* Single document level operations are atomic, consistent, isolated and durable.
* Open Source under Apache 2 License.

Installing Elasticsearch
------------------------
### Installing Elastic Search on a Windows server
* Pre-requisites: 
	* Java Runtime Environment (JRE) http://www.oracle.com/technetwork/java/javase/downloads/index.html
	* After installing JRE, do yourself a favor and set the JAVA___HOME environment variable
		* My Computer -> Properties -> Advanced tab -> Environment variables button
		* Under System Variables, click Add New
		* Name: JAVA___HOME
		* Path: c:\progra~1\Java\jre7
		* Assumes Java JRE is installed at c:\program files\Java\jre[X]
* You can download the Elasticsearch runtime from the Elasticsearch website and follow the standard installation instructions (it's really quite simple)
* OR, if you'd like the Elasticsearch engine to run as a Windows service, you can download an installer from here: http://ruilopes.com/elasticsearch-setup/
	* Basically the installer just unzips the Elasticsearch package, then creates a Windows service wrapping the engine
	* Install to [DRIVE]:\Elasticsearch (avoid installing to the Program Files directory to help avoid UAC issues)
	* An Elasticsearch service will be created, but you may need to start the service manually (Administrative tools -> Services) and set it to startup automatically
* Confirm Elasticsearch is running by browsing to: http://localhost:9200/_cluster/health
	* If you receive a JSON response with a property named "status" that has a value of "green" or "yellow", all systems are go.
	* By default, Elasticsearch listens on port 9200.

	
### Install a web-based management tool for Elastic Search
* I like the ElasticHQ plugin for cluster/node/index monitoring - http://www.elastichq.org/support_plugin.html		
	* Make sure when browsing to the ElasticHQ url after installation that you use trailing forward slash in url - http://localhost:9200/_plugin/HQ/
* I also like the Sense plugin for Google Chrome for testing queries - https://chrome.google.com/webstore/detail/sense/doinijnbnggojdlcjifpdckfokbbfpbo
* And Inquisitor for testing queries, analyzers and tokenizers - https://github.com/polyfractal/elasticsearch-inquisitor
* Several other options/tools available here: http://www.elasticsearch.org/guide/clients/
	* Scroll to the "Health and Performance Monitoring" and "Front Ends" sections
