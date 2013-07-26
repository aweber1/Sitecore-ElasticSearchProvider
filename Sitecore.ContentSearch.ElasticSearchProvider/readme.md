Sitecore 7 Elasticsearch Provider
=================================

Known Issues
------------
See the Issues page for current known issues and bugs:
https://bitbucket.org/aweber1/sitecore-elasticsearch-provider/issues?status=new&status=open

Installing Elasticsearch
------------------------
* Install Elastic Search on your windows server
	- Pre-requisites: Java Runtime Environment (JRE - http://www.oracle.com/technetwork/java/javase/downloads/index.html)
	- http://ruilopes.com/elasticsearch-setup/
		- install to [DRIVE]:\Elasticsearch (avoid program files to help avoid UAC issues)
		- the service will be installed, but you may need to start the service manually (administrative tools -> services)
	- confirm Elastic is running by browsing to: http://localhost:9200/_cluster/health
	
* Install a front-end for Elastic Search
	- set the JAVA_HOME environment variable
		- my computer -> properties -> advanced tab -> environment variables button
		- under system variables, add new
			- name: JAVA_HOME
			- path: c:\progra~1\Java\jre7
				- assumes Java JRE is installed at c:\program files\Java\jre[X]
	- Recommend ElasticHQ 
		- http://www.elastichq.org/support_plugin.html
		- make sure when browsing to HQ url after installation that you use trailing forward slash in url