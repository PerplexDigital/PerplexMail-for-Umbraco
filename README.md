# PerplexMail

PerplexMail is an open source package for the Umbraco CMS. The package can be installed on any 7.X version of Umbraco. The package adds a new set of features to your Umbraco installation allowing you to configure and send emails with great tracking and logging capabilities.

This repository contains all the core files necessery to build the PerplexMail package. If you are only looking to download the package file for your Umbraco installation, or you are looking for additional information or documentation, please refer to our package page on our.umbraco.org:

https://our.umbraco.org/projects/backoffice-extensions/perplexmail-for-umbraco/

<strong>About the package</strong>
To get started simply download the repository ZIP file and open up the solution. The solution comes with two projects: The PerplexMail library project and the PerplerxMail workflow library project. The former project contains all the core files that the package requires to run. The workflow project is optional as this library is only used when you use the Umbraco Forms feature.

When you rebuild the solution, the generated libraries and the package .zip file are automatically generated for you in the /output/ folder. The /input/ folder contains three items: 

/PerplexMail/ folder: Contains all the html/css/js front end files that control the layout in Umbraco
Package.xml file: This file includes information regarding the included Umbraco objects (data types, doctypes, etc.)
Readme.txt: The Umbraco installer text is included in this file.
